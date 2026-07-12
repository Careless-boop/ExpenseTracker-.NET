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
    public record UpdatePersonalTransactionCommand(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid CategoryId
    ) : IRequest;

    public class UpdatePersonalTransactionCommandValidator : AbstractValidator<UpdatePersonalTransactionCommand>
    {
        public UpdatePersonalTransactionCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero");
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description != null);
            RuleFor(x => x.Date).NotEmpty().WithMessage("Date is required");
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.CategoryId).NotEmpty().WithMessage("Category is required");
        }
    }

    public class UpdatePersonalTransactionCommandHandler : IRequestHandler<UpdatePersonalTransactionCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdatePersonalTransactionCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdatePersonalTransactionCommand request,
            CancellationToken cancellationToken)
        {
            var transaction = await _context.PersonalTransactions
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (transaction == null || transaction.UserId != _currentUser.UserId)
                throw new NotFoundException(nameof(PersonalTransaction), request.Id);

            var category = await _context.PersonalCategories
                .FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (category == null || category.UserId != _currentUser.UserId)
                throw new NotFoundException(nameof(PersonalCategory), request.CategoryId);

            transaction.Amount = request.Amount;
            transaction.Description = request.Description;
            transaction.Date = request.Date;
            transaction.Type = request.Type;
            transaction.CategoryId = request.CategoryId;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
