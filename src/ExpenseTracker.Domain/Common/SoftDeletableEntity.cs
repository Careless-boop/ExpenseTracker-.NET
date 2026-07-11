namespace ExpenseTracker.Domain.Common
{
    public abstract class SoftDeletableEntity : AuditableEntity, ISoftDelete
    {
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }
    }
}
