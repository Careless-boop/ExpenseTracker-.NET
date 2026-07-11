using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ExpenseListTransactionConfiguration : IEntityTypeConfiguration<ExpenseListTransaction>
    {
        public void Configure(EntityTypeBuilder<ExpenseListTransaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.CreatedByUserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.HasOne(t => t.ExpenseList)
                .WithMany(l => l.Transactions)
                .HasForeignKey(t => t.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(t => t.PaidByMember)
                .WithMany()
                .HasForeignKey(t => t.PaidByMemberId)
                .OnDelete(DeleteBehavior.Restrict);

            // SetNull would create a second cascade path (ExpenseLists → Categories → Transactions)
            // alongside the direct ExpenseLists → Transactions cascade. SQL Server forbids this.
            // Category reassignment is handled in the app layer (DeleteExpenseListCategory command).
            builder.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(t => !t.IsDeleted);

            builder.HasIndex(t => t.ExpenseListId);
            builder.HasIndex(t => t.PaidByMemberId);
            builder.HasIndex(t => t.Date);
            builder.HasIndex(t => new { t.ExpenseListId, t.Date });
        }
    }
}
