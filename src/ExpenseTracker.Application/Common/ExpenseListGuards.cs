using ExpenseTracker.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Common
{
    internal static class ExpenseListGuards
    {
        /// <summary>
        /// A closed list has already been projected into every member's personal ledger, so letting
        /// it keep changing would silently desync those figures.
        /// </summary>
        public static async Task EnsureNotClosedAsync(
            this IApplicationDbContext context,
            Guid expenseListId,
            CancellationToken cancellationToken)
        {
            var isClosed = await context.ExpenseLists
                .Where(l => l.Id == expenseListId)
                .Select(l => l.ClosedAt != null)
                .FirstOrDefaultAsync(cancellationToken);

            if (isClosed)
                throw new ValidationException([new ValidationFailure(
                    "ExpenseListId",
                    "This expense list is closed. Reopen it to make changes.")]);
        }
    }
}
