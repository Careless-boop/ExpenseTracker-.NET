using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Icon)
                .HasMaxLength(50);

            builder.Property(c => c.Color)
                .HasMaxLength(7);

            builder.Property(c => c.UserId)
                .HasMaxLength(450);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(c => c.ExpenseList)
                .WithMany(l => l.Categories)
                .HasForeignKey(c => c.ExpenseListId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => c.ExpenseListId);
            builder.HasIndex(c => new { c.UserId, c.IsDefault });
            builder.HasIndex(c => new { c.ExpenseListId, c.IsDefault });

            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Category_Ownership",
                "(\"UserId\" IS NOT NULL AND \"ExpenseListId\" IS NULL) OR (\"UserId\" IS NULL AND \"ExpenseListId\" IS NOT NULL)"));
        }
    }
}
