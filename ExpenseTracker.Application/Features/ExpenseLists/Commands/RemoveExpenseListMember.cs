using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record RemoveExpenseListMemberCommand(
        Guid ExpenseListId,
        string UserId
    ) : IRequest;

    public class RemoveExpenseListMemberCommandHandler : IRequestHandler<RemoveExpenseListMemberCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public RemoveExpenseListMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            RemoveExpenseListMemberCommand request,
            CancellationToken cancellationToken)
        {
            var currentMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (currentMembership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            var targetMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == request.UserId,
                    cancellationToken);

            if (targetMembership == null)
            {
                throw new NotFoundException("Member", request.UserId);
            }

            var isSelf = request.UserId == _currentUser.UserId;
            var isOwner = currentMembership.Role == ExpenseListRole.Owner;

            if (!isSelf && !isOwner)
            {
                throw new ForbiddenException("Only the owner can remove other members.");
            }

            if (isSelf && currentMembership.Role == ExpenseListRole.Owner)
            {
                throw new ValidationException([new ValidationFailure(
                nameof(request.UserId),
                "Owner cannot leave. Transfer ownership first or delete the list.")]);
            }

            _context.ExpenseListMembers.Remove(targetMembership);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
