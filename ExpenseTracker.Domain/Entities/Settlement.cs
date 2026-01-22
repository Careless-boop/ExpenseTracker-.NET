using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class Settlement : AuditableEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public string FromUserId { get; set; } = null!;

        public string ToUserId { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime SettledAt { get; set; }

        public string? Note { get; set; }
    }
}
