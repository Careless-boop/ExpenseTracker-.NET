using ExpenseTracker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseTracker.Infrastructure.Persistence.Configurations
{
    public class ExpenseListTransactionParticipantConfiguration : IEntityTypeConfiguration<ExpenseListTransactionParticipant>
    {
        public void Configure(EntityTypeBuilder<ExpenseListTransactionParticipant> builder)
        {
            builder.HasKey(p => p.Id);

            builder.Property(p => p.CustomShareAmount)
                .HasPrecision(18, 2);

            builder.HasOne(p => p.Transaction)
                .WithMany(t => t.Participants)
                .HasForeignKey(p => p.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasQueryFilter(p => !p.IsDeleted);

            builder.HasIndex(p => p.TransactionId);
            builder.HasIndex(p => p.MemberId);
            // Uniqueness applies to active participants only; a soft-deleted row must not block
            // re-adding the same member to a split in a later edit.
            builder.HasIndex(p => new { p.TransactionId, p.MemberId })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");
        }
    }
}
