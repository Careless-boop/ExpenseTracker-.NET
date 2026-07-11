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

namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Commands
{
    public record UpdateExpenseListTransactionCommand(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid PaidByMemberId,
        Guid? CategoryId,
        IReadOnlyList<ParticipantInput>? Participants
    ) : IRequest;

    public class UpdateExpenseListTransactionCommandValidator
        : AbstractValidator<UpdateExpenseListTransactionCommand>
    {
        public UpdateExpenseListTransactionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
            RuleFor(x => x.Date).NotEmpty();
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.PaidByMemberId).NotEmpty();
            RuleForEach(x => x.Participants)
                .ChildRules(p =>
                {
                    p.RuleFor(x => x.MemberId).NotEmpty();
                    p.RuleFor(x => x.CustomShareAmount)
                        .GreaterThan(0).When(x => x.CustomShareAmount.HasValue);
                });
        }
    }

    public class UpdateExpenseListTransactionCommandHandler : IRequestHandler<UpdateExpenseListTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateExpenseListTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateExpenseListTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.ExpenseListTransactions
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null)
                throw new NotFoundException(nameof(ExpenseListTransaction), request.Id);

            var currentMembership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == transaction.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (currentMembership == null || !currentMembership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to update transactions.");

            var listMemberIds = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == transaction.ExpenseListId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (!listMemberIds.Contains(request.PaidByMemberId))
                throw new ValidationException([new ValidationFailure(
                    nameof(request.PaidByMemberId),
                    "Payer is not a member of this expense list")]);

            if (request.CategoryId.HasValue)
            {
                var categoryExists = await _context.ExpenseListCategories
                    .AnyAsync(c =>
                        c.Id == request.CategoryId.Value &&
                        c.ExpenseListId == transaction.ExpenseListId,
                        cancellationToken);
                if (!categoryExists)
                    throw new ValidationException([new ValidationFailure(
                        nameof(request.CategoryId),
                        "Category does not belong to this expense list")]);
            }

            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.Date = request.Date;
            transaction.Type = request.Type;
            transaction.PaidByMemberId = request.PaidByMemberId;
            transaction.CategoryId = request.CategoryId;

            // Replace participants
            foreach (var participant in transaction.Participants.ToList())
                _context.ExpenseListTransactionParticipants.Remove(participant);

            if (request.Participants != null && request.Participants.Count > 0)
            {
                var participantMemberIds = request.Participants.Select(p => p.MemberId).ToList();
                var invalidIds = participantMemberIds.Except(listMemberIds).ToList();
                if (invalidIds.Count > 0)
                    throw new ValidationException([new ValidationFailure(
                        nameof(request.Participants),
                        "One or more participants are not members of this expense list")]);

                foreach (var p in request.Participants)
                {
                    transaction.Participants.Add(new ExpenseListTransactionParticipant
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        MemberId = p.MemberId,
                        CustomShareAmount = p.CustomShareAmount
                    });
                }
            }
            else
            {
                foreach (var memberId in listMemberIds)
                {
                    transaction.Participants.Add(new ExpenseListTransactionParticipant
                    {
                        Id = Guid.NewGuid(),
                        TransactionId = transaction.Id,
                        MemberId = memberId,
                        CustomShareAmount = null
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
