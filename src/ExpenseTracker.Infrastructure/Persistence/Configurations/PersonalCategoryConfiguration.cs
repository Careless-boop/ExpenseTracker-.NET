using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class PersonalCategoryConfiguration : IEntityTypeConfiguration<PersonalCategory>
    {
        public void Configure(EntityTypeBuilder<PersonalCategory> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(c => c.Name)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(c => c.Icon)
                .HasMaxLength(50);

            builder.Property(c => c.Color)
                .HasMaxLength(7);

            builder.HasOne<ApplicationUser>()
                .WithMany(u => u.PersonalCategories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasQueryFilter(c => !c.IsDeleted);

            builder.HasIndex(c => c.UserId);
            builder.HasIndex(c => new { c.UserId, c.IsDefault });
        }
    }
}
