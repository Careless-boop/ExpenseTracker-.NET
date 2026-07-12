using ExpenseTracker.Application.Common;
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
    public record CreateExpenseListTransactionCommand(
        Guid ExpenseListId,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid PaidByMemberId,
        Guid? CategoryId,
        IReadOnlyList<ParticipantInput>? Participants
    ) : IRequest<Guid>;

    public record ParticipantInput(Guid MemberId, decimal? CustomShareAmount);

    public class CreateExpenseListTransactionCommandValidator
        : AbstractValidator<CreateExpenseListTransactionCommand>
    {
        public CreateExpenseListTransactionCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than 0");
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
            RuleFor(x => x.Participants)
                .Must(p => p == null || p.Select(x => x.MemberId).Distinct().Count() == p.Count)
                .WithMessage("A member cannot appear twice in the same split.");
            RuleFor(x => x.Participants)
                .Must((cmd, participants) => ParticipantSplitRules.SharesReconcile(participants, cmd.Amount))
                .WithMessage(ParticipantSplitRules.Message);
        }
    }

    public class CreateExpenseListTransactionCommandHandler
        : IRequestHandler<CreateExpenseListTransactionCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public CreateExpenseListTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task<Guid> Handle(
            CreateExpenseListTransactionCommand request,
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
                throw new ForbiddenException("You need Editor or Owner role to add transactions.");

            await _context.EnsureNotClosedAsync(request.ExpenseListId, cancellationToken);

            var listMemberIds = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.ExpenseListId)
                .Select(m => m.Id)
                .ToListAsync(cancellationToken);

            if (!listMemberIds.Contains(request.PaidByMemberId))
                throw new ValidationException([new ValidationFailure(
                    nameof(request.PaidByMemberId),
                    "Payer is not a member of this expense list")]);

            var categoryId = request.CategoryId;
            if (categoryId == null)
            {
                categoryId = await _defaultCategoryService
                    .GetOrCreateDefaultExpenseListCategoryAsync(request.ExpenseListId, cancellationToken);
            }
            else
            {
                var categoryExists = await _context.ExpenseListCategories
                    .AnyAsync(c => c.Id == categoryId && c.ExpenseListId == request.ExpenseListId,
                        cancellationToken);
                if (!categoryExists)
                    throw new ValidationException([new ValidationFailure(
                        nameof(request.CategoryId),
                        "Category does not belong to this expense list")]);
            }

            var transaction = new ExpenseListTransaction
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                CreatedByUserId = _currentUser.UserId!,
                PaidByMemberId = request.PaidByMemberId,
                Amount = request.Amount,
                Description = request.Description,
                Date = request.Date,
                Type = request.Type,
                CategoryId = categoryId
            };

            if (request.Participants != null && request.Participants.Count > 0)
            {
                var participantMemberIds = request.Participants.Select(p => p.MemberId).ToList();
                var invalidIds = participantMemberIds.Except(listMemberIds).ToList();
                if (invalidIds.Count > 0)
                    throw new ValidationException([new ValidationFailure(
                        nameof(request.Participants),
                        "One or more participants are not members of this expense list")]);

                foreach (var participant in request.Participants)
                {
                    transaction.Participants.Add(new ExpenseListTransactionParticipant
                    {
                        Id = Guid.NewGuid(),
                        MemberId = participant.MemberId,
                        CustomShareAmount = participant.CustomShareAmount
                    });
                }
            }
            else
            {
                // Default: all members participate equally
                foreach (var memberId in listMemberIds)
                {
                    transaction.Participants.Add(new ExpenseListTransactionParticipant
                    {
                        Id = Guid.NewGuid(),
                        MemberId = memberId,
                        CustomShareAmount = null
                    });
                }
            }

            _context.ExpenseListTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
