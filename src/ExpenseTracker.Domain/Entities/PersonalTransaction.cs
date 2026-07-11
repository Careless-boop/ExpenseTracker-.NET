using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities
{
    public class PersonalTransaction : SoftDeletableEntity
    {
        public string UserId { get; set; } = null!;

        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime Date { get; set; }
        public TransactionType Type { get; set; }

        public Guid? CategoryId { get; set; }
        public PersonalCategory? Category { get; set; }
    }
}
