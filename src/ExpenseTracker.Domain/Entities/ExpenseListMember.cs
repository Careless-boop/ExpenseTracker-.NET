using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseListMember : SoftDeletableEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public string DisplayName { get; set; } = null!;

        public string? UserId { get; set; }
        public string? Email { get; set; }

        public ExpenseListRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        public bool IsMock => UserId == null;
        public bool CanEdit => Role >= ExpenseListRole.Editor;
        public bool CanManage => Role == ExpenseListRole.Owner;
    }
}
