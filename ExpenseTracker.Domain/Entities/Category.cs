using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class Category : AuditableEntity, ISoftDelete
    {
        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }

        public string? UserId { get; set; }
        public Guid? ExpenseListId { get; set; }
        public ExpenseList? ExpenseList { get; set; }

        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public bool IsPersonal => UserId != null && ExpenseListId == null;
        public bool IsListOwned => ExpenseListId != null && UserId == null;
    }
}
