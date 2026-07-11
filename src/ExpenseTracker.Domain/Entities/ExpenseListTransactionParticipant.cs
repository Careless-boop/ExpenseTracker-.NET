using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class ExpenseListTransactionParticipant : SoftDeletableEntity
    {
        public Guid TransactionId { get; set; }
        public ExpenseListTransaction Transaction { get; set; } = null!;

        public Guid MemberId { get; set; }
        public ExpenseListMember Member { get; set; } = null!;

        public decimal? CustomShareAmount { get; set; }
    }
}
