using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record DeleteExpenseListCommand(Guid Id) : IRequest;

    public class DeleteExpenseListCommandValidator : AbstractValidator<DeleteExpenseListCommand>
    {
        public DeleteExpenseListCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteExpenseListCommandHandler : IRequestHandler<DeleteExpenseListCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteExpenseListCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeleteExpenseListCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.Id &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership?.ExpenseList == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.Id);
            }

            if (membership.Role != ExpenseListRole.Owner)
            {
                throw new ForbiddenException("Only the owner can delete an expense list.");
            }

            // Soft-delete does not cascade. The interceptor rewrites Deleted into Modified, so the
            // database's ON DELETE CASCADE never fires, and EF only converts children it is already
            // tracking. Removing just the list row would leave its members, transactions and
            // settlements with IsDeleted = false, still visible to every query that does not join
            // through the list. So the children are collected and retired explicitly.
            var participants = await _context.ExpenseListTransactionParticipants
                .Where(p => p.Transaction.ExpenseListId == request.Id)
                .ToListAsync(cancellationToken);

            var transactions = await _context.ExpenseListTransactions
                .Where(t => t.ExpenseListId == request.Id)
                .ToListAsync(cancellationToken);

            var settlements = await _context.Settlements
                .Where(s => s.ExpenseListId == request.Id)
                .ToListAsync(cancellationToken);

            var categories = await _context.ExpenseListCategories
                .Where(c => c.ExpenseListId == request.Id)
                .ToListAsync(cancellationToken);

            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.Id)
                .ToListAsync(cancellationToken);

            await using var dbTransaction = await _context.BeginTransactionAsync(cancellationToken);

            _context.ExpenseListTransactionParticipants.RemoveRange(participants);
            _context.ExpenseListTransactions.RemoveRange(transactions);
            _context.Settlements.RemoveRange(settlements);
            _context.ExpenseListCategories.RemoveRange(categories);
            _context.ExpenseListMembers.RemoveRange(members);
            _context.ExpenseLists.Remove(membership.ExpenseList);

            await _context.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);
        }
    }
}
