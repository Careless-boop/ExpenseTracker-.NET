using ExpenseTracker.Domain.Common;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseListMember : BaseEntity
    {
        public Guid ExpenseListId { get; set; }
        public ExpenseList ExpenseList { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public ExpenseListRole Role { get; set; }
        public DateTime JoinedAt { get; set; }

        public bool CanEdit => Role >= ExpenseListRole.Editor;
        public bool CanManage => Role == ExpenseListRole.Owner;
    }
}
