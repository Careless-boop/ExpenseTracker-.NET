using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities
{
    public class Transaction : AuditableEntity, ISoftDelete
    {
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }

        public string CreatedByUserId { get; set; } = null!;

        public string PaidByUserId { get; set; } = null!;

        public Guid CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public Guid? ExpenseListId { get; set; }
        public ExpenseList? ExpenseList { get; set; }

        public ICollection<TransactionParticipant> Participants { get; set; } = new List<TransactionParticipant>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public bool IsPersonal => ExpenseListId == null;
        public bool IsShared => ExpenseListId != null;
        public bool HasSplit => Participants.Count > 0;

        /// <summary>
        /// Calculate each participant's share. Returns empty if no participants.
        /// Handles equal splits, custom amounts, and mixed scenarios with proper remainder distribution.
        /// </summary>
        public IReadOnlyDictionary<string, decimal> CalculateShares()
        {
            if (Participants.Count == 0)
            {
                return new Dictionary<string, decimal>();
            }

            var shares = new Dictionary<string, decimal>();
            var participantsList = Participants.ToList();

            var customParticipants = participantsList.Where(p => p.CustomShareAmount.HasValue).ToList();
            var equalParticipants = participantsList.Where(p => !p.CustomShareAmount.HasValue).ToList();

            if (equalParticipants.Count == 0)
            {
                foreach (var p in customParticipants)
                {
                    shares[p.UserId] = p.CustomShareAmount!.Value;
                }
            }
            else if (customParticipants.Count == 0)
            {
                var equalShare = Math.Floor(Amount / participantsList.Count * 100) / 100;
                var totalDistributed = equalShare * participantsList.Count;
                var remainder = Amount - totalDistributed;

                var isFirst = true;
                foreach (var p in participantsList)
                {
                    shares[p.UserId] = isFirst ? equalShare + remainder : equalShare;
                    isFirst = false;
                }
            }
            else
            {
                var customTotal = customParticipants.Sum(p => p.CustomShareAmount!.Value);
                var remainingAmount = Amount - customTotal;

                foreach (var p in customParticipants)
                {
                    shares[p.UserId] = p.CustomShareAmount!.Value;
                }

                if (remainingAmount > 0 && equalParticipants.Count > 0)
                {
                    var equalShare = Math.Floor(remainingAmount / equalParticipants.Count * 100) / 100;
                    var totalDistributed = equalShare * equalParticipants.Count;
                    var remainder = remainingAmount - totalDistributed;

                    var isFirst = true;
                    foreach (var p in equalParticipants)
                    {
                        shares[p.UserId] = isFirst ? equalShare + remainder : equalShare;
                        isFirst = false;
                    }
                }
                else if (remainingAmount <= 0)
                {
                    foreach (var p in equalParticipants)
                    {
                        shares[p.UserId] = 0;
                    }
                }
            }

            return shares;
        }
    }
}
