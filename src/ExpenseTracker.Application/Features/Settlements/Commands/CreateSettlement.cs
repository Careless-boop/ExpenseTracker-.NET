using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;

namespace ExpenseTracker.Application.Features.Settlements.Commands
{
    public record CreateSettlementCommand(
        Guid ExpenseListId,
        Guid ToMemberId,
        decimal Amount,
        string? Note = null
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

            var currentMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == currentUserId,
                    cancellationToken);

            if (currentMembership == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            var toMember = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.Id == request.ToMemberId,
                    cancellationToken);

            if (toMember == null)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ToMemberId),
                "Recipient is not a member of this expense list")]);

            if (currentMembership.Id == request.ToMemberId)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ToMemberId),
                "Cannot settle with yourself")]);

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                FromMemberId = currentMembership.Id,
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
