using ExpenseTracker.Domain.Entities;
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

            builder.Property(s => s.Note)
                .HasMaxLength(500);

            builder.HasOne(s => s.ExpenseList)
                .WithMany(l => l.Settlements)
                .HasForeignKey(s => s.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.FromMember)
                .WithMany()
                .HasForeignKey(s => s.FromMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(s => s.ToMember)
                .WithMany()
                .HasForeignKey(s => s.ToMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(s => !s.IsDeleted);

            builder.HasIndex(s => s.ExpenseListId);
            builder.HasIndex(s => s.FromMemberId);
            builder.HasIndex(s => s.ToMemberId);
            builder.HasIndex(s => s.SettledAt);
        }
    }
}
