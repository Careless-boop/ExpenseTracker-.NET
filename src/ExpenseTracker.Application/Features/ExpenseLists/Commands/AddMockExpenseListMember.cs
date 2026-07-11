using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record AddMockExpenseListMemberCommand(
        Guid ExpenseListId,
        string DisplayName
    ) : IRequest<Guid>;

    public class AddMockExpenseListMemberCommandValidator : AbstractValidator<AddMockExpenseListMemberCommand>
    {
        public AddMockExpenseListMemberCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage("Display name is required")
                .MaximumLength(100).WithMessage("Display name must not exceed 100 characters");
        }
    }

    public class AddMockExpenseListMemberCommandHandler : IRequestHandler<AddMockExpenseListMemberCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public AddMockExpenseListMemberCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            AddMockExpenseListMemberCommand request,
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
                throw new ForbiddenException("You need Editor or Owner role to add mock members.");

            var mockMember = new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                UserId = null,
                DisplayName = request.DisplayName,
                Role = ExpenseListRole.Viewer,
                JoinedAt = DateTime.UtcNow
            };

            _context.ExpenseListMembers.Add(mockMember);
            await _context.SaveChangesAsync(cancellationToken);

            return mockMember.Id;
        }
    }
}
