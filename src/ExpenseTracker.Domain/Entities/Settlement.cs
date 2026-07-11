using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class Settlement : SoftDeletableEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public Guid FromMemberId { get; set; }
        public ExpenseListMember FromMember { get; set; } = null!;

        public Guid ToMemberId { get; set; }
        public ExpenseListMember ToMember { get; set; } = null!;

        public decimal Amount { get; set; }

        public DateTime SettledAt { get; set; }

        public string? Note { get; set; }
    }
}
