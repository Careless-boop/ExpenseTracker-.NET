using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
    {
        public void Configure(EntityTypeBuilder<Settlement> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(s => s.FromUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(s => s.ToUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(s => s.Note)
                .HasMaxLength(500);

            builder.HasOne(s => s.ExpenseList)
                .WithMany(l => l.Settlements)
                .HasForeignKey(s => s.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(s => s.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(s => s.ExpenseListId);
            builder.HasIndex(s => s.FromUserId);
            builder.HasIndex(s => s.ToUserId);
            builder.HasIndex(s => s.SettledAt);
        }
    }
}
