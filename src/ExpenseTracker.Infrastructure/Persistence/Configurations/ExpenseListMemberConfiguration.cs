using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ExpenseListMemberConfiguration : IEntityTypeConfiguration<ExpenseListMember>
    {
        public void Configure(EntityTypeBuilder<ExpenseListMember> builder)
        {
            builder.HasKey(m => m.Id);

            builder.Property(m => m.DisplayName)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(m => m.UserId)
                .HasMaxLength(450);

            builder.Property(m => m.Email)
                .HasMaxLength(256);

            builder.HasOne(m => m.ExpenseList)
                .WithMany(l => l.Members)
                .HasForeignKey(m => m.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.ExpenseListMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired(false);

            builder.HasQueryFilter(m => !m.IsDeleted);

            builder.HasIndex(m => m.ExpenseListId);
            builder.HasIndex(m => m.UserId);
            // One real-user membership per list. Mock members have a null UserId so they don't
            // conflict. IsDeleted must be in the filter: removals are soft, so the tombstone row
            // still carries the UserId and would collide on re-add or on a mock-member merge.
            builder.HasIndex(m => new { m.ExpenseListId, m.UserId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL AND [IsDeleted] = 0");
        }
    }
}
