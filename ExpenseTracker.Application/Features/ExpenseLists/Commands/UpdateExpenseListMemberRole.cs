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
    public record UpdateExpenseListMemberRoleCommand(
        Guid ExpenseListId,
        string UserId,
        ExpenseListRole NewRole
    ) : IRequest;

    public class UpdateExpenseListMemberRoleCommandValidator : AbstractValidator<UpdateExpenseListMemberRoleCommand>
    {
        public UpdateExpenseListMemberRoleCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.NewRole).IsInEnum();
        }
    }

    public class UpdateExpenseListMemberRoleCommandHandler : IRequestHandler<UpdateExpenseListMemberRoleCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateExpenseListMemberRoleCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateExpenseListMemberRoleCommand request,
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

            if (currentMembership.Role != ExpenseListRole.Owner)
            {
                throw new ForbiddenException("Only the owner can change member roles.");
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

            if (request.UserId == _currentUser.UserId)
            {
                throw new ValidationException([new ValidationFailure(
                nameof(request.UserId),
                "Cannot change your own role. Transfer ownership instead.")]);
            }

            if (request.NewRole == ExpenseListRole.Owner)
            {
                currentMembership.Role = ExpenseListRole.Editor;
            }

            targetMembership.Role = request.NewRole;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
