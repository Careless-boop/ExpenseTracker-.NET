using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Transactions
{
    public record DeletePersonalTransactionCommand(Guid Id) : IRequest;

    public class DeletePersonalTransactionCommandHandler : IRequestHandler<DeletePersonalTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public DeletePersonalTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            DeletePersonalTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.PersonalTransactions
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null || transaction.UserId != _currentUser.UserId)
                throw new NotFoundException(nameof(PersonalTransaction), request.Id);

            _context.PersonalTransactions.Remove(transaction);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
