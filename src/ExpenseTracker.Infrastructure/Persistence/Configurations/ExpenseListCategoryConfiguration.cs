using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ExpenseListCategoryConfiguration : IEntityTypeConfiguration<ExpenseListCategory>
    {
        public void Configure(EntityTypeBuilder<ExpenseListCategory> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Icon)
                .HasMaxLength(50);

            builder.Property(c => c.Color)
                .HasMaxLength(7);

            builder.HasOne(c => c.ExpenseList)
                .WithMany(l => l.Categories)
                .HasForeignKey(c => c.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.HasIndex(c => c.ExpenseListId);
            builder.HasIndex(c => new { c.ExpenseListId, c.IsDefault });
        }
    }
}
