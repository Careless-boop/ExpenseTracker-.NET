using ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.UnitTests
{
    public class UpdateExpenseListTransactionTests : IDisposable
    {
        private readonly TestDatabase _db = new("user-alice");
        private readonly Guid _listId = Guid.NewGuid();
        private readonly Guid _alice;
        private readonly Guid _bob;
        private readonly Guid _carol;
        private readonly Guid _txId = Guid.NewGuid();

        public UpdateExpenseListTransactionTests()
        {
            _db.AddUser("user-alice");

            _db.Context.ExpenseLists.Add(new ExpenseList { Id = _listId, Name = "Trip" });

            _alice = AddMember("Alice", "user-alice", ExpenseListRole.Owner);
            _bob = AddMember("Bob", null, ExpenseListRole.Viewer);
            _carol = AddMember("Carol", null, ExpenseListRole.Viewer);

            var tx = new ExpenseListTransaction
            {
                Id = _txId,
                ExpenseListId = _listId,
                CreatedByUserId = "user-alice",
                PaidByMemberId = _alice,
                Amount = 90m,
                Date = DateTime.UtcNow,
                Type = TransactionType.Expense
            };
            foreach (var memberId in new[] { _alice, _bob, _carol })
                tx.Participants.Add(new ExpenseListTransactionParticipant
                {
                    Id = Guid.NewGuid(),
                    TransactionId = _txId,
                    MemberId = memberId,
                    CustomShareAmount = null
                });

            _db.Context.ExpenseListTransactions.Add(tx);
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

        private UpdateExpenseListTransactionCommand Command(
            IReadOnlyList<ParticipantInput>? participants, decimal amount = 90m) =>
            new(_txId, amount, "Dinner", DateTime.UtcNow, TransactionType.Expense, _alice, null, participants);

        // Each edit runs against its own context, exactly as a request does.
        private async Task Save(IReadOnlyList<ParticipantInput>? participants)
        {
            await using var ctx = _db.NewContext();
            await new UpdateExpenseListTransactionCommandHandler(ctx, _db.CurrentUser)
                .Handle(Command(participants), default);
        }

        private async Task<List<ExpenseListTransactionParticipant>> ActiveParticipants()
        {
            await using var ctx = _db.NewContext();
            return await ctx.ExpenseListTransactionParticipants
                .Where(p => p.TransactionId == _txId)
                .ToListAsync();
        }

        private static ParticipantInput P(Guid memberId, decimal? custom = null) => new(memberId, custom);

        [Fact]
        public async Task Saving_with_the_same_participants_does_not_throw()
        {
            // Regression: re-inserting the same (TransactionId, MemberId) rows tripped a circular
            // dependency in SaveChanges because participants are soft-deleted, not hard-deleted.
            var act = () => Save(new[] { P(_alice), P(_bob), P(_carol) });

            await act.Should().NotThrowAsync();
            (await ActiveParticipants()).Should().HaveCount(3);
        }

        [Fact]
        public async Task Changing_a_custom_share_updates_in_place()
        {
            await Save(new[] { P(_alice, 40m), P(_bob), P(_carol) });

            var participants = await ActiveParticipants();
            participants.Should().HaveCount(3);
            participants.Single(p => p.MemberId == _alice).CustomShareAmount.Should().Be(40m);
        }

        [Fact]
        public async Task Removing_a_participant_drops_only_that_row()
        {
            await Save(new[] { P(_alice), P(_bob) });

            var participants = await ActiveParticipants();
            participants.Should().HaveCount(2);
            participants.Select(p => p.MemberId).Should().NotContain(_carol);
        }

        [Fact]
        public async Task Adding_a_member_not_previously_in_the_split_inserts_a_row()
        {
            // Regression: the participant Id is store-generated, so adding a pre-keyed child to the
            // tracked transaction made EF emit an UPDATE (0 rows) instead of an INSERT.
            await Save(new[] { P(_alice) });

            var act = () => Save(new[] { P(_alice), P(_bob) });

            await act.Should().NotThrowAsync();
            (await ActiveParticipants()).Select(p => p.MemberId)
                .Should().BeEquivalentTo(new[] { _alice, _bob });
        }

        [Fact]
        public async Task Removing_then_re_adding_a_member_across_edits_succeeds()
        {
            // A soft-deleted participant must not block re-adding that member: the unique index is
            // filtered to active rows, so the re-add is a clean INSERT.
            await Save(new[] { P(_alice), P(_bob) });

            var act = () => Save(new[] { P(_alice), P(_bob), P(_carol) });

            await act.Should().NotThrowAsync();
            (await ActiveParticipants()).Select(p => p.MemberId)
                .Should().BeEquivalentTo(new[] { _alice, _bob, _carol });
        }
    }
}
