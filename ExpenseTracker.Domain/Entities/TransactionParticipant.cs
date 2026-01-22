using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class TransactionParticipant : BaseEntity
    {
        public Guid TransactionId { get; set; }
        public Transaction Transaction { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public decimal? CustomShareAmount { get; set; }
    }
}
