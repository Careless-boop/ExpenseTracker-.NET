using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseList : AuditableEntity, ISoftDelete
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }

        public ICollection<ExpenseListMember> Members { get; set; } = new List<ExpenseListMember>();
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();

        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
