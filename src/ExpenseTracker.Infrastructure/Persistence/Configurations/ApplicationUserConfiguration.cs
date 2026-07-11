using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(u => u.DisplayName).HasMaxLength(100);
            builder.Property(u => u.AvatarUrl).HasMaxLength(500);

            builder.Property(u => u.RefreshTokenHash).HasMaxLength(64);
            builder.HasIndex(u => u.RefreshTokenHash);
        }
    }
}
