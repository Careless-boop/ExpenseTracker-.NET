using ExpenseTracker.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace ExpenseTracker.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser
    {
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<Transaction> CreatedTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> PaidTransactions { get; set; } = new List<Transaction>();
        public ICollection<ExpenseListMember> ExpenseListMemberships { get; set; } = new List<ExpenseListMember>();
    }
}
