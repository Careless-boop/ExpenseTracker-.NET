using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Services;
using FluentAssertions;

namespace ExpenseTracker.UnitTests
{
    public class BalanceCalculationServiceTests : IDisposable
    {
        private readonly TestDatabase _db = new();
        private readonly BalanceCalculationService _service;
        private readonly Guid _listId = Guid.NewGuid();

        public BalanceCalculationServiceTests()
        {
            _service = new BalanceCalculationService(_db.Context);

            _db.Context.ExpenseLists.Add(new ExpenseList { Id = _listId, Name = "Trip" });
        }

        public void Dispose() => _db.Dispose();

        private Guid AddMember(string name, string? userId = null)
        {
            if (userId != null)
                _db.AddUser(userId);

            var member = new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                DisplayName = name,
                UserId = userId,
                Role = ExpenseListRole.Editor,
                JoinedAt = DateTime.UtcNow
            };

            _db.Context.ExpenseListMembers.Add(member);
            return member.Id;
        }

        private void AddTransaction(
            decimal amount,
            Guid paidBy,
            TransactionType type,
            params Guid[] participants)
        {
            var transaction = new ExpenseListTransaction
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                CreatedByUserId = "user-alice",
                PaidByMemberId = paidBy,
                Amount = amount,
                Date = DateTime.UtcNow,
                Type = type
            };

            foreach (var memberId in participants)
            {
                transaction.Participants.Add(new ExpenseListTransactionParticipant
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId
                });
            }

            _db.Context.ExpenseListTransactions.Add(transaction);
        }

        [Fact]
        public async Task Balances_sum_to_zero_and_reflect_who_paid()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");
            var carol = AddMember("Carol");

            AddTransaction(90m, alice, TransactionType.Expense, alice, bob, carol);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Sum(m => m.Balance).Should().Be(0m);
            result.Members.Single(m => m.MemberId == alice).Balance.Should().Be(60m);
            result.Members.Single(m => m.MemberId == bob).Balance.Should().Be(-30m);
            result.Members.Single(m => m.MemberId == carol).Balance.Should().Be(-30m);
        }

        [Fact]
        public async Task Income_moves_the_opposite_way_to_an_expense()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            // Alice collected 100 on the group's behalf, so she is holding 50 that belongs to Bob.
            AddTransaction(100m, alice, TransactionType.Income, alice, bob);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Single(m => m.MemberId == alice).Balance.Should().Be(-50m);
            result.Members.Single(m => m.MemberId == bob).Balance.Should().Be(50m);
            result.Members.Sum(m => m.Balance).Should().Be(0m);
        }

        [Fact]
        public async Task An_expense_and_an_equal_income_cancel_out()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            AddTransaction(100m, alice, TransactionType.Expense, alice, bob);
            AddTransaction(100m, alice, TransactionType.Income, alice, bob);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Should().OnlyContain(m => m.Balance == 0m);
        }

        [Fact]
        public async Task Balance_equals_total_paid_minus_total_share()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            AddTransaction(70m, alice, TransactionType.Expense, alice, bob);
            AddTransaction(30m, bob, TransactionType.Expense, alice, bob);
            AddTransaction(20m, alice, TransactionType.Income, alice, bob);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Should().OnlyContain(m => m.Balance == m.TotalPaid - m.TotalShare);
        }

        [Fact]
        public async Task Total_expense_share_ignores_income()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            AddTransaction(100m, alice, TransactionType.Expense, alice, bob);
            AddTransaction(40m, alice, TransactionType.Income, alice, bob);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            // What Alice actually consumed is her half of the expense, not netted against income.
            result.Members.Single(m => m.MemberId == alice).TotalExpenseShare.Should().Be(50m);
            result.Members.Single(m => m.MemberId == bob).TotalExpenseShare.Should().Be(50m);
        }

        [Fact]
        public async Task A_transaction_with_no_participants_is_excluded_everywhere()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            AddTransaction(100m, alice, TransactionType.Expense);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Should().OnlyContain(m => m.Balance == 0m && m.TotalPaid == 0m);
        }

        [Fact]
        public async Task A_settlement_moves_the_debtor_toward_zero()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");

            AddTransaction(100m, alice, TransactionType.Expense, alice, bob);

            _db.Context.Settlements.Add(new Settlement
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                FromMemberId = bob,
                ToMemberId = alice,
                Amount = 50m,
                SettledAt = DateTime.UtcNow
            });

            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.Members.Should().OnlyContain(m => m.Balance == 0m);
            result.SimplifiedDebts.Should().BeEmpty();
        }

        [Fact]
        public async Task Simplified_debts_conserve_the_total_and_stay_under_n_transfers()
        {
            var alice = AddMember("Alice", "user-alice");
            var bob = AddMember("Bob", "user-bob");
            var carol = AddMember("Carol");
            var dave = AddMember("Dave");

            AddTransaction(100m, alice, TransactionType.Expense, alice, bob, carol, dave);
            AddTransaction(40m, bob, TransactionType.Expense, alice, bob, carol, dave);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.SimplifiedDebts.Should().HaveCountLessThan(result.Members.Count);

            var owedToCreditors = result.Members.Where(m => m.Balance > 0).Sum(m => m.Balance);
            result.SimplifiedDebts.Sum(d => d.Amount).Should().Be(owedToCreditors);
        }

        [Fact]
        public async Task Totals_report_expenses_and_income_separately()
        {
            var alice = AddMember("Alice", "user-alice");

            AddTransaction(100m, alice, TransactionType.Expense, alice);
            AddTransaction(25m, alice, TransactionType.Income, alice);
            await _db.Context.SaveChangesAsync();

            var result = await _service.CalculateAsync(_listId);

            result.TotalExpenses.Should().Be(100m);
            result.TotalIncome.Should().Be(25m);
        }
    }
}
