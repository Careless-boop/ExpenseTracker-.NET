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

        /// <summary>SHA-256 of the issued refresh token, hex-encoded. The raw token is never stored.</summary>
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

        public ICollection<PersonalTransaction> PersonalTransactions { get; set; } = new List<PersonalTransaction>();
        public ICollection<PersonalCategory> PersonalCategories { get; set; } = new List<PersonalCategory>();
        public ICollection<ExpenseListMember> ExpenseListMemberships { get; set; } = new List<ExpenseListMember>();
    }
}
