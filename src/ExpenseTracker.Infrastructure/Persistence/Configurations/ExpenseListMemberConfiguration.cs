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

            builder.Property(m => m.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasOne(m => m.ExpenseList)
                .WithMany(l => l.Members)
                .HasForeignKey(m => m.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.ExpenseListMemberships)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(m => new { m.ExpenseListId, m.UserId })
                .IsUnique();

            builder.HasIndex(m => m.UserId);
            builder.HasIndex(m => m.ExpenseListId);
        }
    }
}
