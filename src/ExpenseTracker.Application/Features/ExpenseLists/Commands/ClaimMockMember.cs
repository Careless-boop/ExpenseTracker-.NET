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
    public record ClaimMockMemberCommand(
        Guid ExpenseListId,
        Guid MockMemberId
    ) : IRequest;

    public class ClaimMockMemberCommandValidator : AbstractValidator<ClaimMockMemberCommand>
    {
        public ClaimMockMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.MockMemberId).NotEmpty();
        }
    }

    public class ClaimMockMemberCommandHandler : IRequestHandler<ClaimMockMemberCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public ClaimMockMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task Handle(
            ClaimMockMemberCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;

            var alreadyMember = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == currentUserId,
                    cancellationToken);

            if (alreadyMember)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.MockMemberId),
                    "You are already a member of this expense list")]);

            var mockMember = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.Id == request.MockMemberId &&
                    m.ExpenseListId == request.ExpenseListId,
                    cancellationToken);

            if (mockMember == null)
                throw new NotFoundException(nameof(ExpenseListMember), request.MockMemberId);

            if (!mockMember.IsMock)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.MockMemberId),
                    "This slot is already claimed by a registered user")]);

            var user = await _identityService.GetUserAsync(currentUserId);

            mockMember.UserId = currentUserId;
            mockMember.DisplayName = user?.DisplayName ?? user?.UserName ?? currentUserId;
            mockMember.Email = user?.Email;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
