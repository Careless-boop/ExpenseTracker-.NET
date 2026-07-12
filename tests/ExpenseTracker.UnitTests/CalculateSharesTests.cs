using ExpenseTracker.Domain.Entities;
using FluentAssertions;

namespace ExpenseTracker.UnitTests
{
    public class CalculateSharesTests
    {
        private static ExpenseListTransaction Transaction(
            decimal amount,
            params (Guid MemberId, decimal? Custom)[] participants)
        {
            var transaction = new ExpenseListTransaction { Amount = amount };

            foreach (var (memberId, custom) in participants)
            {
                transaction.Participants.Add(new ExpenseListTransactionParticipant
                {
                    Id = Guid.NewGuid(),
                    MemberId = memberId,
                    CustomShareAmount = custom
                });
            }

            return transaction;
        }

        private static Guid Member(byte seed) =>
            new([seed, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0]);

        [Fact]
        public void No_participants_yields_no_shares()
        {
            Transaction(100m).CalculateShares().Should().BeEmpty();
        }

        [Fact]
        public void Equal_split_that_divides_cleanly()
        {
            var shares = Transaction(90m, (Member(1), null), (Member(2), null), (Member(3), null))
                .CalculateShares();

            shares.Values.Should().AllBeEquivalentTo(30m);
        }

        [Theory]
        [InlineData(10.01, 3)]
        [InlineData(100.00, 3)]
        [InlineData(0.01, 2)]
        [InlineData(10.00, 3)]
        [InlineData(0.05, 4)]
        [InlineData(1234.56, 7)]
        public void Equal_split_always_conserves_the_total(decimal amount, int memberCount)
        {
            var participants = Enumerable.Range(1, memberCount)
                .Select(i => (Member((byte)i), (decimal?)null))
                .ToArray();

            var shares = Transaction(amount, participants).CalculateShares();

            shares.Values.Sum().Should().Be(amount);
            shares.Should().HaveCount(memberCount);
        }

        [Fact]
        public void Equal_split_remainder_lands_on_the_same_member_every_time()
        {
            // The odd cent must not wander: EF hands back Participants in no guaranteed order, and a
            // balance that shifts by a cent between two reads of unchanged data is a real bug.
            var transaction = Transaction(10.01m, (Member(9), null), (Member(2), null));

            var first = transaction.CalculateShares();
            var second = transaction.CalculateShares();

            first.Should().BeEquivalentTo(second);
            first[Member(2)].Should().Be(5.01m, "the lowest MemberId absorbs the remainder");
            first[Member(9)].Should().Be(5.00m);
        }

        [Fact]
        public void All_custom_shares_are_used_verbatim_when_they_sum_to_the_total()
        {
            var shares = Transaction(100m,
                (Member(1), 70m),
                (Member(2), 30m)).CalculateShares();

            shares[Member(1)].Should().Be(70m);
            shares[Member(2)].Should().Be(30m);
            shares.Values.Sum().Should().Be(100m);
        }

        [Fact]
        public void All_custom_shares_that_under_allocate_still_conserve_the_total()
        {
            // Validators reject this at the API boundary, but rows written before they existed can
            // look like this, and a balance sheet that loses 80.00 is worse than an ugly share.
            var shares = Transaction(100m,
                (Member(1), 10m),
                (Member(2), 10m)).CalculateShares();

            shares.Values.Sum().Should().Be(100m);
        }

        [Fact]
        public void All_custom_shares_that_over_allocate_still_conserve_the_total()
        {
            var shares = Transaction(100m,
                (Member(1), 80m),
                (Member(2), 80m)).CalculateShares();

            shares.Values.Sum().Should().Be(100m);
        }

        [Fact]
        public void Mixed_custom_and_equal_splits_the_remainder_among_the_rest()
        {
            var shares = Transaction(100m,
                (Member(1), 40m),
                (Member(2), null),
                (Member(3), null)).CalculateShares();

            shares[Member(1)].Should().Be(40m);
            shares[Member(2)].Should().Be(30m);
            shares[Member(3)].Should().Be(30m);
            shares.Values.Sum().Should().Be(100m);
        }

        [Fact]
        public void Mixed_split_with_an_indivisible_remainder_conserves_the_total()
        {
            var shares = Transaction(100.00m,
                (Member(1), 49.99m),
                (Member(2), null),
                (Member(3), null)).CalculateShares();

            shares.Values.Sum().Should().Be(100.00m);
        }

        [Fact]
        public void Mixed_split_where_custom_shares_consume_everything_gives_the_rest_nothing()
        {
            var shares = Transaction(100m,
                (Member(1), 100m),
                (Member(2), null)).CalculateShares();

            shares[Member(2)].Should().Be(0m);
            shares.Values.Sum().Should().Be(100m);
        }

        [Fact]
        public void Single_participant_owes_the_whole_amount()
        {
            var shares = Transaction(33.33m, (Member(1), null)).CalculateShares();

            shares[Member(1)].Should().Be(33.33m);
        }
    }
}
