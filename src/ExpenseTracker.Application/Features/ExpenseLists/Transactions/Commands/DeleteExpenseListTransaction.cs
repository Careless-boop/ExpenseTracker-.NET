using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands
{
    public record DeleteExpenseListTransactionCommand(Guid Id) : IRequest;

    public class DeleteExpenseListTransactionCommandValidator
        : AbstractValidator<DeleteExpenseListTransactionCommand>
    {
        public DeleteExpenseListTransactionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteExpenseListTransactionCommandHandler
        : IRequestHandler<DeleteExpenseListTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteExpenseListTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeleteExpenseListTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.ExpenseListTransactions
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null)
                throw new NotFoundException(nameof(ExpenseListTransaction), request.Id);

            var currentMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == transaction.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (currentMembership == null || !currentMembership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to delete transactions.");

            await _context.EnsureNotClosedAsync(transaction.ExpenseListId, cancellationToken);

            // Soft-delete participants first (interceptor handles IsDeleted via Remove)
            foreach (var participant in transaction.Participants.ToList())
                _context.ExpenseListTransactionParticipants.Remove(participant);

            // Soft-delete the transaction itself
            _context.ExpenseListTransactions.Remove(transaction);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
