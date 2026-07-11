using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseListCategory : SoftDeletableEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }

        public ICollection<ExpenseListTransaction> Transactions { get; set; } = new List<ExpenseListTransaction>();
    }
}
