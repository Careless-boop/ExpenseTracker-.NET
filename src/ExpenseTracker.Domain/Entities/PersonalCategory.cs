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

        /// <summary>Set when this category was auto-created by closing a shared list, so it can be found by id rather than by a name that may collide.</summary>
        public Guid? SourceExpenseListId { get; set; }

        public ICollection<PersonalTransaction> Transactions { get; set; } = new List<PersonalTransaction>();
    }
}
