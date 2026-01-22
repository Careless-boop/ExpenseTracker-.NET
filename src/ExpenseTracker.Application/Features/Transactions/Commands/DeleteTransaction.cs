using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Transactions.Commands
{
    public record DeleteTransactionCommand(Guid Id) : IRequest;

    public class DeleteTransactionCommandHandler : IRequestHandler<DeleteTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeleteTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeleteTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.Transactions
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null)
            {
                throw new NotFoundException(nameof(Transaction), request.Id);
            }

            if (transaction.IsPersonal)
            {
                if (transaction.CreatedByUserId != _currentUser.UserId)
                {
                    throw new ForbiddenException();
                }
            }
            else
            {
                var membership = await _context.ExpenseListMembers
                    .FirstOrDefaultAsync(m =>
                        m.ExpenseListId == transaction.ExpenseListId &&
                        m.UserId == _currentUser.UserId,
                        cancellationToken);

                if (membership == null || !membership.CanEdit)
                {
                    throw new ForbiddenException("You need Editor or Owner role to delete transactions.");
                }
            }

            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
