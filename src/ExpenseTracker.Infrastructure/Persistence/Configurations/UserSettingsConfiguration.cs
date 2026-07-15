using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
    {
        public void Configure(EntityTypeBuilder<UserSettings> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(s => s.SyncClosedListsToPersonal)
                .HasDefaultValue(true);

            builder.Property(s => s.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();

            builder.HasIndex(s => s.UserId).IsUnique();

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
