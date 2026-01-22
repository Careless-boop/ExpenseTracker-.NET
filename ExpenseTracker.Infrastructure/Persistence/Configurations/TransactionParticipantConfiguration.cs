using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class TransactionParticipantConfiguration : IEntityTypeConfiguration<TransactionParticipant>
    {
        public void Configure(EntityTypeBuilder<TransactionParticipant> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.UserId)
                .HasMaxLength(450)
                .IsRequired();

            builder.Property(p => p.CustomShareAmount)
                .HasPrecision(18, 2);

            builder.HasOne(p => p.Transaction)
                .WithMany(t => t.Participants)
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<ApplicationUser>()
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(p => new { p.TransactionId, p.UserId })
                .IsUnique();

            builder.HasIndex(p => p.TransactionId);
            builder.HasIndex(p => p.UserId);
        }
    }
}
