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
    public record UpdateTransactionCommand(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid CategoryId,
        string? PaidByUserId = null,
        List<string>? ParticipantUserIds = null
    ) : IRequest;

    public class UpdateTransactionCommandValidator : AbstractValidator<UpdateTransactionCommand>
    {
        public UpdateTransactionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();

            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("Amount must be greater than zero");

            RuleFor(x => x.Description)
                .MaximumLength(500).When(x => x.Description != null);

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("Category is required");

            RuleFor(x => x.ParticipantUserIds)
                .Must(p => p == null || p.Count > 0)
                .WithMessage("If specifying participants, at least one is required");
        }
    }

    public class UpdateTransactionCommandHandler : IRequestHandler<UpdateTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.Transactions
                .Include(t => t.Participants)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null)
            {
                throw new NotFoundException(nameof(Transaction), request.Id);
            }

            if (transaction.IsPersonal)
            {
                if (transaction.CreatedByUserId != _currentUser.UserId)
                {
                    throw new ForbiddenException();
                }
            }
            else
            {
                var membership = await _context.ExpenseListMembers
                    .FirstOrDefaultAsync(m =>
                        m.ExpenseListId == transaction.ExpenseListId &&
                        m.UserId == _currentUser.UserId,
                        cancellationToken);

                if (membership == null || !membership.CanEdit)
                {
                    throw new ForbiddenException("You need Editor or Owner role to update transactions.");
                }
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null)
            {
                throw new NotFoundException(nameof(Category), request.CategoryId);
            }

            if (transaction.IsShared)
            {
                if (category.ExpenseListId != transaction.ExpenseListId)
                {
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(request.CategoryId),
                    "Category must belong to the same expense list")]);
                }
            }
            else
            {
                if (category.UserId != _currentUser.UserId || category.ExpenseListId != null)
                {
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(request.CategoryId),
                    "Category must be your personal category")]);
                }
            }

            if (transaction.IsShared && request.PaidByUserId != null)
            {
                var payerIsMember = await _context.ExpenseListMembers
                    .AnyAsync(m =>
                        m.ExpenseListId == transaction.ExpenseListId &&
                        m.UserId == request.PaidByUserId,
                        cancellationToken);

                if (!payerIsMember)
                {
                    throw new ValidationException([new FluentValidation.Results.ValidationFailure(
                    nameof(request.PaidByUserId),
                    "Payer must be a member of the expense list")]);
                }
            }

            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.Date = request.Date;
            transaction.Type = request.Type;
            transaction.CategoryId = request.CategoryId;

            if (request.PaidByUserId != null)
            {
                transaction.PaidByUserId = request.PaidByUserId;
            }

            if (transaction.IsShared)
            {
                _context.TransactionParticipants.RemoveRange(transaction.Participants);
                transaction.Participants.Clear();

                if (request.ParticipantUserIds != null && request.ParticipantUserIds.Count > 0)
                {
                    var memberUserIds = await _context.ExpenseListMembers
                        .Where(m => m.ExpenseListId == transaction.ExpenseListId)
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
                            TransactionId = transaction.Id,
                            UserId = participantUserId,
                            CustomShareAmount = null 
                        });
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
