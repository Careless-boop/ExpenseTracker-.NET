using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record DeleteExpenseListCommand(Guid Id) : IRequest;

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

            if (membership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.Id);
            }

            if (membership.Role != ExpenseListRole.Owner)
            {
                throw new ForbiddenException("Only the owner can delete an expense list.");
            }

            _context.ExpenseLists.Remove(membership.ExpenseList);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
