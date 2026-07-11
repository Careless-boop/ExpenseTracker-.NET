using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.ExpenseLists.Categories.Commands
{
    public record DeleteExpenseListCategoryCommand(Guid Id, Guid? ReplacementCategoryId = null) : IRequest;

    public class DeleteExpenseListCategoryCommandValidator
        : AbstractValidator<DeleteExpenseListCategoryCommand>
    {
        public DeleteExpenseListCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteExpenseListCategoryCommandHandler : IRequestHandler<DeleteExpenseListCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public DeleteExpenseListCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task Handle(
            DeleteExpenseListCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.ExpenseListCategories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
                throw new NotFoundException(nameof(ExpenseListCategory), request.Id);

            if (category.IsDefault)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.Id), "Cannot delete the default category")]);

            var membership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == category.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null || !membership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to manage categories.");

            // Reassign transactions to replacement or default category
            var targetCategoryId = request.ReplacementCategoryId;
            if (targetCategoryId == null)
            {
                targetCategoryId = await _defaultCategoryService
                    .GetOrCreateDefaultExpenseListCategoryAsync(category.ExpenseListId, cancellationToken);
            }

            await _context.ExpenseListTransactions
                .Where(t => t.CategoryId == category.Id)
                .ExecuteUpdateAsync(s =>
                    s.SetProperty(t => t.CategoryId, targetCategoryId),
                    cancellationToken);

            _context.ExpenseListCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
