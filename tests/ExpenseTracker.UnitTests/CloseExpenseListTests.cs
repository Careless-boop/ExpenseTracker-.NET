using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Features.ExpenseLists.Commands;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.UnitTests
{
    public class CloseExpenseListTests : IDisposable
    {
        private readonly TestDatabase _db = new("user-alice");
        private readonly Guid _listId = Guid.NewGuid();
        private readonly Guid _alice;
        private readonly Guid _bob;
        private readonly Guid _carolMock;

        public CloseExpenseListTests()
        {
            _db.AddUser("user-alice");
            _db.AddUser("user-bob");

            _db.Context.ExpenseLists.Add(new ExpenseList { Id = _listId, Name = "Ski Trip" });

            _alice = AddMember("Alice", "user-alice", ExpenseListRole.Owner);
            _bob = AddMember("Bob", "user-bob", ExpenseListRole.Editor);
            _carolMock = AddMember("Carol", null, ExpenseListRole.Viewer);

            // 90 expense split three ways => each consumed 30.
            var transaction = new ExpenseListTransaction
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                CreatedByUserId = "user-alice",
                PaidByMemberId = _alice,
                Amount = 90m,
                Date = DateTime.UtcNow,
                Type = TransactionType.Expense
            };

            foreach (var memberId in new[] { _alice, _bob, _carolMock })
            {
                transaction.Participants.Add(new ExpenseListTransactionParticipant
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId
                });
            }

            _db.Context.ExpenseListTransactions.Add(transaction);
            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private Guid AddMember(string name, string? userId, ExpenseListRole role)
        {
            var member = new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                DisplayName = name,
                UserId = userId,
                Role = role,
                JoinedAt = DateTime.UtcNow
            };

            _db.Context.ExpenseListMembers.Add(member);
            return member.Id;
        }

        private CloseExpenseListCommandHandler CloseHandler() =>
            new(_db.Context, _db.CurrentUser, new BalanceCalculationService(_db.Context));

        private ReopenExpenseListCommandHandler ReopenHandler() =>
            new(_db.Context, _db.CurrentUser);

        [Fact]
        public async Task Closing_files_each_real_member_s_share_into_their_personal_ledger()
        {
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            var personal = await _db.Context.PersonalTransactions.ToListAsync();

            personal.Should().HaveCount(2, "the mock member has no personal ledger");
            personal.Should().OnlyContain(t => t.Amount == 30m);
            personal.Should().OnlyContain(t => t.Type == TransactionType.Expense);
            personal.Should().OnlyContain(t => t.SourceExpenseListId == _listId);
            personal.Select(t => t.UserId).Should().BeEquivalentTo(["user-alice", "user-bob"]);

            var categories = await _db.Context.PersonalCategories
                .Where(c => c.SourceExpenseListId == _listId)
                .ToListAsync();

            categories.Should().HaveCount(2);
            categories.Should().OnlyContain(c => c.Name == "Ski Trip");
        }

        [Fact]
        public async Task Closing_marks_the_list_closed()
        {
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            var list = await _db.Context.ExpenseLists.FindAsync(_listId);
            list!.IsClosed.Should().BeTrue();
            list.ClosedByUserId.Should().Be("user-alice");
        }

        [Fact]
        public async Task A_member_who_opted_out_gets_nothing()
        {
            _db.Context.UserSettings.Add(new UserSettings
            {
                Id = Guid.NewGuid(),
                UserId = "user-bob",
                SyncClosedListsToPersonal = false
            });
            await _db.Context.SaveChangesAsync();

            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            var personal = await _db.Context.PersonalTransactions.ToListAsync();
            personal.Should().ContainSingle().Which.UserId.Should().Be("user-alice");
        }

        [Fact]
        public async Task Only_the_owner_can_close()
        {
            _db.SignInAs("user-bob");

            var act = () => CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            await act.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task Closing_twice_is_refused()
        {
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            var act = () => CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Reopening_withdraws_the_projected_transactions()
        {
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);
            await ReopenHandler().Handle(new ReopenExpenseListCommand(_listId), default);

            // Soft-deleted, so the query filter should hide them entirely.
            (await _db.Context.PersonalTransactions.ToListAsync()).Should().BeEmpty();

            var list = await _db.Context.ExpenseLists.FindAsync(_listId);
            list!.IsClosed.Should().BeFalse();
        }

        [Fact]
        public async Task Close_reopen_close_does_not_double_count()
        {
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);
            await ReopenHandler().Handle(new ReopenExpenseListCommand(_listId), default);
            await CloseHandler().Handle(new CloseExpenseListCommand(_listId), default);

            var personal = await _db.Context.PersonalTransactions.ToListAsync();

            personal.Should().HaveCount(2, "reopening withdrew the first projection");
            personal.Should().OnlyContain(t => t.Amount == 30m);
        }
    }
}
