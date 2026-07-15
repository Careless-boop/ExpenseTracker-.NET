using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ExpenseListConfiguration : IEntityTypeConfiguration<ExpenseList>
    {
        public void Configure(EntityTypeBuilder<ExpenseList> builder)
        {
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasMaxLength(1000);

            builder.Property(e => e.CoverImage)
                .HasMaxLength(500);

            builder.Property(e => e.Currency)
                .HasMaxLength(3)
                .HasDefaultValue("USD")
                .IsRequired();

            builder.HasQueryFilter(e => !e.IsDeleted);
        }
    }
}
