using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    /// <summary>
    /// Reverses a close: the list becomes writable again and the personal transactions it projected
    /// are withdrawn, so reopening does not leave members double-counting the list in their ledger.
    /// </summary>
    public record ReopenExpenseListCommand(Guid ExpenseListId) : IRequest;

    public class ReopenExpenseListCommandValidator : AbstractValidator<ReopenExpenseListCommand>
    {
        public ReopenExpenseListCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
        }
    }

    public class ReopenExpenseListCommandHandler : IRequestHandler<ReopenExpenseListCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public ReopenExpenseListCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            ReopenExpenseListCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership?.ExpenseList == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            if (membership.Role != ExpenseListRole.Owner)
                throw new ForbiddenException("Only the owner can reopen an expense list.");

            var expenseList = membership.ExpenseList;

            if (!expenseList.IsClosed)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.ExpenseListId), "This expense list is not closed.")]);

            var projected = await _context.PersonalTransactions
                .Where(t => t.SourceExpenseListId == expenseList.Id)
                .ToListAsync(cancellationToken);

            await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);

            // Soft-deleted via the interceptor. The auto-created categories are left alone: they may
            // now hold transactions the user filed there by hand.
            _context.PersonalTransactions.RemoveRange(projected);

            expenseList.ClosedAt = null;
            expenseList.ClosedByUserId = null;

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
    }
}
