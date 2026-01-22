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
        string ToUserId,
        decimal Amount,
        string? Note = null
    ) : IRequest<Guid>;

    public class CreateSettlementCommandValidator : AbstractValidator<CreateSettlementCommand>
    {
        public CreateSettlementCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.ToUserId).NotEmpty();
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
                .Where(m => m.UserId == currentUserId || m.UserId == request.ToUserId)
                .ToListAsync(cancellationToken);

            if (!members.Any(m => m.UserId == currentUserId))
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            if (!members.Any(m => m.UserId == request.ToUserId))
            {
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ToUserId),
                "Recipient must be a member of the expense list")]);
            }

            if (currentUserId == request.ToUserId)
                throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                nameof(request.ToUserId),
                "Cannot settle with yourself")]);

            var settlement = new Settlement
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                FromUserId = currentUserId,
                ToUserId = request.ToUserId,
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
