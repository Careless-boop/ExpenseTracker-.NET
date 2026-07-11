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

        public bool HasSplit => Participants.Count > 0;

        /// <summary>
        /// Calculate each participant's share keyed by MemberId.
        /// Handles equal splits, custom amounts, and mixed scenarios with proper remainder distribution.
        /// </summary>
        public IReadOnlyDictionary<Guid, decimal> CalculateShares()
        {
            if (Participants.Count == 0)
                return new Dictionary<Guid, decimal>();

            var shares = new Dictionary<Guid, decimal>();
            var participantsList = Participants.ToList();

            var customParticipants = participantsList.Where(p => p.CustomShareAmount.HasValue).ToList();
            var equalParticipants = participantsList.Where(p => !p.CustomShareAmount.HasValue).ToList();

            if (equalParticipants.Count == 0)
            {
                foreach (var p in customParticipants)
                    shares[p.MemberId] = p.CustomShareAmount!.Value;
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

                if (remainingAmount > 0 && equalParticipants.Count > 0)
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
                else if (remainingAmount <= 0)
                {
                    foreach (var p in equalParticipants)
                        shares[p.MemberId] = 0;
                }
            }

            return shares;
        }
    }
}
