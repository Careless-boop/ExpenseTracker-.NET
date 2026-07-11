using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record UpdateMockExpenseListMemberCommand(
        Guid ExpenseListId,
        Guid MemberId,
        string DisplayName
    ) : IRequest;

    public class UpdateMockExpenseListMemberCommandValidator
        : AbstractValidator<UpdateMockExpenseListMemberCommand>
    {
        public UpdateMockExpenseListMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.MemberId).NotEmpty();
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");
        }
    }

    public class UpdateMockExpenseListMemberCommandHandler
        : IRequestHandler<UpdateMockExpenseListMemberCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateMockExpenseListMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateMockExpenseListMemberCommand request,
            CancellationToken cancellationToken)
        {
            var currentMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (currentMembership == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            if (!currentMembership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to rename mock members.");

            var member = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.Id == request.MemberId &&
                    m.ExpenseListId == request.ExpenseListId,
                    cancellationToken);

            if (member == null)
                throw new NotFoundException(nameof(ExpenseListMember), request.MemberId);

            // A real user's display name comes from their account, not from the list.
            if (!member.IsMock)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.MemberId),
                    "Only placeholder members can be renamed.")]);

            member.DisplayName = request.DisplayName;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
