using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class PersonalTransactionConfiguration : IEntityTypeConfiguration<PersonalTransaction>
    {
        public void Configure(EntityTypeBuilder<PersonalTransaction> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(t => t.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            builder.Property(t => t.Description)
                .HasMaxLength(500);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.PersonalTransactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // SetNull would create a second cascade path (AspNetUsers → PersonalCategories → PersonalTransactions)
            // alongside the direct AspNetUsers → PersonalTransactions cascade. SQL Server forbids this.
            // Category reassignment is handled in the app layer (DeletePersonalCategory command).
            builder.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(t => !t.IsDeleted);

            builder.HasIndex(t => t.UserId);
            builder.HasIndex(t => t.Date);
            builder.HasIndex(t => new { t.UserId, t.Date });
            builder.HasIndex(t => t.SourceExpenseListId);
        }
    }
}
