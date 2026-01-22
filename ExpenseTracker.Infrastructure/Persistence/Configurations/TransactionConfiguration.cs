using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
    {
        public void Configure(EntityTypeBuilder<Transaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.Property(t => t.CreatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(t => t.PaidByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.CreatedTransactions)
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.PaidTransactions)
                .HasForeignKey(t => t.PaidByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.ExpenseList)
                .WithMany(l => l.Transactions)
                .HasForeignKey(t => t.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(t => !t.IsDeleted);

            builder.HasIndex(t => t.CreatedByUserId);
            builder.HasIndex(t => t.PaidByUserId);
            builder.HasIndex(t => t.CategoryId);
            builder.HasIndex(t => t.ExpenseListId);
            builder.HasIndex(t => t.Date);
            builder.HasIndex(t => new { t.ExpenseListId, t.Date });
        }
    }
}
