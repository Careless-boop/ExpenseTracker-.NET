using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;

namespace ExpenseTracker.Application.Features.Transactions.Commands
{
    public record CreateTransactionCommand(
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid? CategoryId,
        Guid? ExpenseListId,
        string? PaidByUserId,
        List<string>? ParticipantUserIds
    ) : IRequest<Guid>;

    public class CreateTransactionCommandValidator : AbstractValidator<CreateTransactionCommand>
    {
        public CreateTransactionCommandValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");

            RuleFor(x => x.Description)
                .MaximumLength(500).When(x => x.Description != null);

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required");

            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid transaction type");

            RuleFor(x => x.ParticipantUserIds)
                .Must(p => p == null || p.Count > 0)
                .WithMessage("If specifying participants, at least one is required");
        }
    }

    public class CreateTransactionCommandHandler : IRequestHandler<CreateTransactionCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public CreateTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task<Guid> Handle(
            CreateTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;

            if (request.ExpenseListId.HasValue)
            {
                var membership = await _context.ExpenseListMembers
                    .FirstOrDefaultAsync(m =>
                        m.ExpenseListId == request.ExpenseListId.Value &&
                        m.UserId == currentUserId,
                        cancellationToken);

                if (membership == null)
                {
                    throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId.Value);
                }

                if (!membership.CanEdit)
                {
                    throw new ForbiddenException("You need Editor or Owner role to create transactions.");
                }
            }

            var paidByUserId = currentUserId;

            if (request.ExpenseListId.HasValue && !string.IsNullOrEmpty(request.PaidByUserId))
            {
                var payerIsMember = await _context.ExpenseListMembers
                    .AnyAsync(m =>
                        m.ExpenseListId == request.ExpenseListId.Value &&
                        m.UserId == request.PaidByUserId,
                        cancellationToken);

                if (!payerIsMember)
                {
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(request.PaidByUserId),
                    "Payer must be a member of the expense list")]);
                }

                paidByUserId = request.PaidByUserId;
            }

            Guid categoryId;
            if (request.CategoryId.HasValue)
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, cancellationToken);

                if (category == null)
                {
                    throw new NotFoundException(nameof(Category), request.CategoryId.Value);
                }

                if (request.ExpenseListId.HasValue)
                {
                    if (category.ExpenseListId != request.ExpenseListId.Value)
                    {
                        throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                        nameof(request.CategoryId),
                        "Category must belong to the same expense list")]);
                    }
                }
                else
                {
                    if (category.UserId != currentUserId || category.ExpenseListId != null)
                    {
                        throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                        nameof(request.CategoryId),
                        "Category must be your personal category")]);
                    }
                }

                categoryId = category.Id;
            }
            else
            {
                categoryId = await _defaultCategoryService.GetDefaultCategoryIdAsync(
                    request.ExpenseListId.HasValue ? null : currentUserId,
                    request.ExpenseListId,
                    cancellationToken);
            }

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Amount = request.Amount,
                Description = request.Description,
                Date = request.Date,
                Type = request.Type,
                CreatedByUserId = currentUserId,
                PaidByUserId = paidByUserId,
                CategoryId = categoryId,
                ExpenseListId = request.ExpenseListId
            };

            if (request.ExpenseListId.HasValue && request.ParticipantUserIds != null && request.ParticipantUserIds.Count > 0)
            {
                var memberUserIds = await _context.ExpenseListMembers
                    .Where(m => m.ExpenseListId == request.ExpenseListId.Value)
                    .Select(m => m.UserId)
                    .ToListAsync(cancellationToken);

                var invalidParticipants = request.ParticipantUserIds
                    .Where(p => !memberUserIds.Contains(p))
                    .ToList();

                if (invalidParticipants.Count > 0)
                {
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(request.ParticipantUserIds),
                    $"Users not members of this list: {string.Join(", ", invalidParticipants)}")]);
                }

                foreach (var participantUserId in request.ParticipantUserIds)
                {
                    transaction.Participants.Add(new TransactionParticipant
                    {
                        Id = Guid.NewGuid(),
                        UserId = participantUserId,
                        CustomShareAmount = null
                    });
                }
            }

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
