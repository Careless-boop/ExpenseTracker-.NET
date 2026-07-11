using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.Settlements.Commands
{
    /// <summary>
    /// Records that FromMemberId paid ToMemberId. FromMemberId defaults to the caller; supplying a
    /// different one is only allowed for mock members, so a settlement can never be forged in the
    /// name of another real user.
    /// </summary>
    public record CreateSettlementCommand(
        Guid ExpenseListId,
        Guid ToMemberId,
        decimal Amount,
        string? Note = null,
        Guid? FromMemberId = null
    ) : IRequest<Guid>;

    public class CreateSettlementCommandValidator : AbstractValidator<CreateSettlementCommand>
    {
        public CreateSettlementCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.ToMemberId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
            RuleFor(x => x.Note).MaximumLength(500).When(x => x.Note != null);
        }
    }

    public class CreateSettlementCommandHandler : IRequestHandler<CreateSettlementCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateSettlementCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateSettlementCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;

            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.ExpenseListId)
                .ToListAsync(cancellationToken);

            var currentMembership = members.FirstOrDefault(m => m.UserId == currentUserId);

            if (currentMembership == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            if (!currentMembership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to record settlements.");

            var fromMemberId = request.FromMemberId ?? currentMembership.Id;

            if (fromMemberId != currentMembership.Id)
            {
                var fromMember = members.FirstOrDefault(m => m.Id == fromMemberId);

                if (fromMember == null)
                    throw new ValidationException([new ValidationFailure(
                        nameof(request.FromMemberId),
                        "Payer is not a member of this expense list")]);

                if (!fromMember.IsMock)
                    throw new ForbiddenException(
                        "You can only record a settlement on behalf of a placeholder member.");
            }

            if (members.All(m => m.Id != request.ToMemberId))
                throw new ValidationException([new ValidationFailure(
                    nameof(request.ToMemberId),
                    "Recipient is not a member of this expense list")]);

            if (fromMemberId == request.ToMemberId)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.ToMemberId),
                    "Cannot settle with yourself")]);

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                FromMemberId = fromMemberId,
                ToMemberId = request.ToMemberId,
                Amount = request.Amount,
                SettledAt = DateTime.UtcNow,
                Note = request.Note
            };

            _context.Settlements.Add(settlement);
            await _context.SaveChangesAsync(cancellationToken);

            return settlement.Id;
        }
    }
}
