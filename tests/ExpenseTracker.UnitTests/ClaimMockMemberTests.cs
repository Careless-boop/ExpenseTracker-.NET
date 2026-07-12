using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Features.ExpenseLists.Commands;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.UnitTests
{
    public class ClaimMockMemberTests : IDisposable
    {
        private readonly TestDatabase _db = new("user-alice");
        private readonly Guid _listId = Guid.NewGuid();
        private readonly Guid _mockMemberId;

        public ClaimMockMemberTests()
        {
            _db.AddUser("user-alice");
            _db.AddUser("user-bob");

            _db.Context.ExpenseLists.Add(new ExpenseList { Id = _listId, Name = "Trip" });

            _db.Context.ExpenseListMembers.Add(new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                DisplayName = "Alice",
                UserId = "user-alice",
                Role = ExpenseListRole.Owner,
                JoinedAt = DateTime.UtcNow
            });

            _mockMemberId = Guid.NewGuid();
            _db.Context.ExpenseListMembers.Add(new ExpenseListMember
            {
                Id = _mockMemberId,
                ExpenseListId = _listId,
                DisplayName = "Bob (offline)",
                UserId = null,
                Role = ExpenseListRole.Viewer,
                JoinedAt = DateTime.UtcNow
            });

            _db.Context.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private ClaimMockMemberCommandHandler Handler() => new(_db.Context, _db.CurrentUser);

        private Guid AddRealMember(string userId, string name)
        {
            var member = new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                DisplayName = name,
                UserId = userId,
                Email = $"{userId}@test.io",
                Role = ExpenseListRole.Editor,
                JoinedAt = DateTime.UtcNow
            };

            _db.Context.ExpenseListMembers.Add(member);
            _db.Context.SaveChanges();
            return member.Id;
        }

        [Fact]
        public async Task A_non_member_cannot_claim_a_placeholder()
        {
            // The whole point of the redesign: holding two guids must not be enough to join a list.
            _db.SignInAs("user-bob");

            var act = () => Handler().Handle(
                new ClaimMockMemberCommand(_listId, _mockMemberId), default);

            await act.Should().ThrowAsync<ForbiddenException>();

            var mock = await _db.Context.ExpenseListMembers.FindAsync(_mockMemberId);
            mock!.UserId.Should().BeNull("the placeholder must be untouched");
        }

        [Fact]
        public async Task A_member_claiming_a_placeholder_absorbs_its_history()
        {
            var bobMemberId = AddRealMember("user-bob", "Bob");

            // An expense already recorded against the placeholder, before Bob signed up.
            var transaction = new ExpenseListTransaction
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                CreatedByUserId = "user-alice",
                PaidByMemberId = _mockMemberId,
                Amount = 40m,
                Date = DateTime.UtcNow,
                Type = TransactionType.Expense
            };
            _db.Context.ExpenseListTransactions.Add(transaction);
            _db.Context.SaveChanges();

            _db.SignInAs("user-bob");
            await Handler().Handle(new ClaimMockMemberCommand(_listId, _mockMemberId), default);

            var claimed = await _db.Context.ExpenseListMembers.FindAsync(_mockMemberId);
            claimed!.UserId.Should().Be("user-bob");
            claimed.Role.Should().Be(ExpenseListRole.Editor, "the caller's role carries over");

            // Bob now has exactly one membership: his own row was folded into the placeholder.
            var bobMemberships = await _db.Context.ExpenseListMembers
                .Where(m => m.ExpenseListId == _listId && m.UserId == "user-bob")
                .ToListAsync();
            bobMemberships.Should().ContainSingle().Which.Id.Should().Be(_mockMemberId);

            var foldedRow = await _db.Context.ExpenseListMembers
                .IgnoreQueryFilters()
                .SingleAsync(m => m.Id == bobMemberId);
            foldedRow.IsDeleted.Should().BeTrue("the caller's own row is retired, not left as a duplicate");

            var reloaded = await _db.Context.ExpenseListTransactions.FindAsync(transaction.Id);
            reloaded!.PaidByMemberId.Should().Be(_mockMemberId);
        }

        [Fact]
        public async Task A_placeholder_that_was_already_claimed_cannot_be_claimed_again()
        {
            AddRealMember("user-bob", "Bob");
            _db.SignInAs("user-bob");
            await Handler().Handle(new ClaimMockMemberCommand(_listId, _mockMemberId), default);

            _db.AddUser("user-carol");
            AddRealMember("user-carol", "Carol");
            _db.SignInAs("user-carol");

            var act = () => Handler().Handle(
                new ClaimMockMemberCommand(_listId, _mockMemberId), default);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Claiming_is_refused_when_both_members_share_a_transaction()
        {
            var bobMemberId = AddRealMember("user-bob", "Bob");

            // Bob and the placeholder both take a share, so merging them would duplicate a participant.
            var transaction = new ExpenseListTransaction
            {
                Id = Guid.NewGuid(),
                ExpenseListId = _listId,
                CreatedByUserId = "user-alice",
                PaidByMemberId = bobMemberId,
                Amount = 60m,
                Date = DateTime.UtcNow,
                Type = TransactionType.Expense
            };
            transaction.Participants.Add(new ExpenseListTransactionParticipant
            {
                Id = Guid.NewGuid(),
                MemberId = bobMemberId
            });
            transaction.Participants.Add(new ExpenseListTransactionParticipant
            {
                Id = Guid.NewGuid(),
                MemberId = _mockMemberId
            });
            _db.Context.ExpenseListTransactions.Add(transaction);
            _db.Context.SaveChanges();

            _db.SignInAs("user-bob");

            var act = () => Handler().Handle(
                new ClaimMockMemberCommand(_listId, _mockMemberId), default);

            await act.Should().ThrowAsync<ValidationException>();
        }
    }
}
