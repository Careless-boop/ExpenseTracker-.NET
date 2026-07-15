using ExpenseTracker.Domain.Common;

namespace ExpenseTracker.Domain.Entities
{
    public class UserSettings : AuditableEntity
    {
        public string UserId { get; set; } = null!;

        /// <summary>
        /// When a shared list is closed, copy this user's total expense share into their personal
        /// transactions under a category named after the list.
        /// </summary>
        public bool SyncClosedListsToPersonal { get; set; } = true;

        /// <summary>
        /// ISO 4217 code used to format the user's personal ledger, and the default for lists they
        /// create. A display attribute only — amounts are never converted.
        /// </summary>
        public string Currency { get; set; } = "USD";
    }
}
