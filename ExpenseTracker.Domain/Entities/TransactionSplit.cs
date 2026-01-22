using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class TransactionSplit : BaseEntity
    {
        public Guid TransactionId { get; set; }
        public Transaction Transaction { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public decimal Amount { get; set; }

        public bool IsSettled { get; set; }
        public DateTime? SettledAt { get; set; }
    }
}
