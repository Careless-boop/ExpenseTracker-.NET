using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class PersonalCategory : SoftDeletableEntity
    {
        public string UserId { get; set; } = null!;

        public string Name { get; set; } = null!;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }

        public ICollection<PersonalTransaction> Transactions { get; set; } = new List<PersonalTransaction>();
    }
}
