using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseList : SoftDeletableEntity
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? CoverImage { get; set; }

        public DateTime? ClosedAt { get; set; }
        public string? ClosedByUserId { get; set; }

        /// <summary>A closed list is read-only, and its expenses have been pushed to each member's personal ledger.</summary>
        public bool IsClosed => ClosedAt != null;

        public ICollection<ExpenseListMember> Members { get; set; } = new List<ExpenseListMember>();
        public ICollection<ExpenseListTransaction> Transactions { get; set; } = new List<ExpenseListTransaction>();
        public ICollection<ExpenseListCategory> Categories { get; set; } = new List<ExpenseListCategory>();
        public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
    }
}
