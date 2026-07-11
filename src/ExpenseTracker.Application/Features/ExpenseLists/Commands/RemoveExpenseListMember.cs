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
    public record RemoveExpenseListMemberCommand(
        Guid ExpenseListId,
        Guid MemberId
    ) : IRequest;

    public class RemoveExpenseListMemberCommandValidator : AbstractValidator<RemoveExpenseListMemberCommand>
    {
        public RemoveExpenseListMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.MemberId).NotEmpty();
        }
    }

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
                    m.Id == request.MemberId,
                    cancellationToken);

            if (targetMembership == null)
            {
                throw new NotFoundException(nameof(ExpenseListMember), request.MemberId);
            }

            var isSelf = request.MemberId == currentMembership.Id;
            var isOwner = currentMembership.Role == ExpenseListRole.Owner;

            if (!isSelf && !isOwner)
            {
                throw new ForbiddenException("Only the owner can remove other members.");
            }

            if (isSelf && isOwner)
            {
                throw new ValidationException([new ValidationFailure(
                nameof(request.MemberId),
                "Owner cannot leave. Transfer ownership first or delete the list.")]);
            }

            _context.ExpenseListMembers.Remove(targetMembership);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
