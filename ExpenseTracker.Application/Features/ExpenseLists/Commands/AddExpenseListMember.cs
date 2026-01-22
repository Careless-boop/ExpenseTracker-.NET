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
    public record AddExpenseListMemberCommand(
        Guid ExpenseListId,
        string UserEmail,
        ExpenseListRole Role = ExpenseListRole.Editor
    ) : IRequest<Guid>;

    public class AddExpenseListMemberCommandValidator : AbstractValidator<AddExpenseListMemberCommand>
    {
        public AddExpenseListMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.UserEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.Role)
                .IsInEnum()
                .Must(r => r != ExpenseListRole.Owner)
                .WithMessage("Cannot add another owner. Transfer ownership instead.");
        }
    }

    public class AddExpenseListMemberCommandHandler : IRequestHandler<AddExpenseListMemberCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public AddExpenseListMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task<Guid> Handle(
            AddExpenseListMemberCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            if (membership.Role != ExpenseListRole.Owner)
            {
                throw new ForbiddenException("Only the owner can add members.");
            }

            var user = await _identityService.GetUserByEmailAsync(request.UserEmail);
            if (user == null)
            {
                throw new ValidationException([new ValidationFailure(
                nameof(request.UserEmail),
                "User with this email not found")]);
            }

            var existingMembership = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == user.Id,
                    cancellationToken);

            if (existingMembership)
            {
                throw new ValidationException([new ValidationFailure(
                nameof(request.UserEmail),
                "User is already a member of this expense list")]);
            }

            var newMember = new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                UserId = user.Id,
                Role = request.Role,
                JoinedAt = DateTime.UtcNow
            };

            _context.ExpenseListMembers.Add(newMember);
            await _context.SaveChangesAsync(cancellationToken);

            return newMember.Id;
        }
    }
}
