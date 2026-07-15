using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseListTransaction : SoftDeletableEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public string CreatedByUserId { get; set; } = null!;

        public Guid PaidByMemberId { get; set; }
        public ExpenseListMember PaidByMember { get; set; } = null!;

        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }

        public Guid? CategoryId { get; set; }
        public ExpenseListCategory? Category { get; set; }

        public ICollection<ExpenseListTransactionParticipant> Participants { get; set; } = new List<ExpenseListTransactionParticipant>();

        /// <summary>
        /// When every participant has a custom share and those shares fall short of the Amount, the
        /// shortfall is divided equally between them on top of their custom amounts. Without this the
        /// split is rejected. The custom amounts stay stored as entered, so the original intent
        /// ("Viktor 30, Sonya 24, split the other 46") survives a round-trip.
        /// </summary>
        public bool SplitRemainder { get; set; }

        public bool HasSplit => Participants.Count > 0;

        /// <summary>
        /// Calculate each participant's share keyed by MemberId.
        /// Handles equal splits, custom amounts, and mixed scenarios with proper remainder distribution.
        /// Shares always sum to exactly <see cref="Amount"/>.
        /// </summary>
        public IReadOnlyDictionary<Guid, decimal> CalculateShares()
        {
            if (Participants.Count == 0)
                return new Dictionary<Guid, decimal>();

            var shares = new Dictionary<Guid, decimal>();

            // Ordered by MemberId because the rounding remainder lands on the first participant.
            // EF loads Participants in no guaranteed order, so without this the member who absorbs
            // the odd cent could change between two reads of an unchanged transaction.
            var participantsList = Participants.OrderBy(p => p.MemberId).ToList();

            var customParticipants = participantsList.Where(p => p.CustomShareAmount.HasValue).ToList();
            var equalParticipants = participantsList.Where(p => !p.CustomShareAmount.HasValue).ToList();

            if (equalParticipants.Count == 0)
            {
                foreach (var p in customParticipants)
                    shares[p.MemberId] = p.CustomShareAmount!.Value;

                var residual = Amount - customParticipants.Sum(p => p.CustomShareAmount!.Value);

                if (SplitRemainder && residual > 0)
                {
                    // The shortfall is shared equally on top of the custom amounts.
                    var extra = Math.Round(residual / customParticipants.Count, 2, MidpointRounding.ToNegativeInfinity);
                    var odd = residual - extra * customParticipants.Count;

                    var isFirstCustom = true;
                    foreach (var p in customParticipants)
                    {
                        shares[p.MemberId] += isFirstCustom ? extra + odd : extra;
                        isFirstCustom = false;
                    }
                }
                // Validators reject custom shares that don't sum to Amount, but rows predating them
                // may. Balances must still conserve, so any residual lands on the first participant.
                else if (residual != 0)
                {
                    shares[customParticipants[0].MemberId] += residual;
                }
            }
            else if (customParticipants.Count == 0)
            {
                var equalShare = Math.Round(Amount / participantsList.Count, 2, MidpointRounding.ToNegativeInfinity);
                var remainder = Amount - equalShare * participantsList.Count;

                var isFirst = true;
                foreach (var p in participantsList)
                {
                    shares[p.MemberId] = isFirst ? equalShare + remainder : equalShare;
                    isFirst = false;
                }
            }
            else
            {
                var customTotal = customParticipants.Sum(p => p.CustomShareAmount!.Value);
                var remainingAmount = Amount - customTotal;

                foreach (var p in customParticipants)
                    shares[p.MemberId] = p.CustomShareAmount!.Value;

                if (remainingAmount > 0)
                {
                    var equalShare = Math.Round(remainingAmount / equalParticipants.Count, 2, MidpointRounding.ToNegativeInfinity);
                    var remainder = remainingAmount - equalShare * equalParticipants.Count;

                    var isFirst = true;
                    foreach (var p in equalParticipants)
                    {
                        shares[p.MemberId] = isFirst ? equalShare + remainder : equalShare;
                        isFirst = false;
                    }
                }
                else
                {
                    foreach (var p in equalParticipants)
                        shares[p.MemberId] = 0;

                    // Custom shares over-allocate; push the overshoot back onto the first of them.
                    if (remainingAmount < 0)
                        shares[customParticipants[0].MemberId] += remainingAmount;
                }
            }

            return shares;
        }
    }
}
