using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Transactions
{
    public record CreatePersonalTransactionCommand(
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid? CategoryId
    ) : IRequest<Guid>;

    public class CreatePersonalTransactionCommandValidator : AbstractValidator<CreatePersonalTransactionCommand>
    {
        public CreatePersonalTransactionCommandValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
            RuleFor(x => x.Date).NotEmpty().WithMessage("Date is required");
            RuleFor(x => x.Type).IsInEnum().WithMessage("Invalid transaction type");
        }
    }

    public class CreatePersonalTransactionCommandHandler : IRequestHandler<CreatePersonalTransactionCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public CreatePersonalTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task<Guid> Handle(
            CreatePersonalTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;

            Guid categoryId;
            if (request.CategoryId.HasValue)
            {
                var category = await _context.PersonalCategories
                    .FirstOrDefaultAsync(c => c.Id == request.CategoryId.Value, cancellationToken);

                if (category == null || category.UserId != currentUserId)
                    throw new NotFoundException(nameof(PersonalCategory), request.CategoryId.Value);

                categoryId = category.Id;
            }
            else
            {
                categoryId = await _defaultCategoryService.GetOrCreateDefaultPersonalCategoryAsync(
                    currentUserId, cancellationToken);
            }

            var transaction = new PersonalTransaction
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Amount = request.Amount,
                Description = request.Description,
                Date = request.Date,
                Type = request.Type,
                CategoryId = categoryId
            };

            _context.PersonalTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            return transaction.Id;
        }
    }
}
