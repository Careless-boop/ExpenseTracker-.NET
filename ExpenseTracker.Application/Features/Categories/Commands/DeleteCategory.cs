using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using ValidationException = ExpenseTracker.Application.Common.Exceptions.ValidationException;
using ValidationFailure = FluentValidation.Results.ValidationFailure;

namespace ExpenseTracker.Application.Features.Categories.Commands
{
    public record DeleteCategoryCommand(
        Guid Id,
        Guid? ReassignToCategoryId = null
    ) : IRequest;

    public class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
    {
        public DeleteCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public DeleteCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task Handle(
            DeleteCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
            {
                throw new NotFoundException(nameof(Category), request.Id);
            }

            if (category.IsDefault)
            {
                throw new ForbiddenException("Default category cannot be deleted.");
            }

            if (category.IsPersonal)
            {
                if (category.UserId != _currentUser.UserId)
                {
                    throw new ForbiddenException();
                }
            }
            else if (category.IsListOwned)
            {
                var membership = await _context.ExpenseListMembers
                    .FirstOrDefaultAsync(m =>
                        m.ExpenseListId == category.ExpenseListId &&
                        m.UserId == _currentUser.UserId,
                        cancellationToken);

                if (membership == null || !membership.CanEdit)
                {
                    throw new ForbiddenException("You need Editor or Owner role to delete categories.");
                }
            }

            Guid targetCategoryId;
            if (request.ReassignToCategoryId.HasValue)
            {
                var targetCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == request.ReassignToCategoryId.Value, cancellationToken);

                if (targetCategory == null)
                {
                    throw new NotFoundException(nameof(Category), request.ReassignToCategoryId.Value);
                }

                if (category.UserId != targetCategory.UserId ||
                    category.ExpenseListId != targetCategory.ExpenseListId)
                {
                    throw new ValidationException(
                        [new ValidationFailure(
                        nameof(request.ReassignToCategoryId),
                        "Target category must be in the same context")]);
                }

                targetCategoryId = targetCategory.Id;
            }
            else
            {
                targetCategoryId = await _defaultCategoryService.GetDefaultCategoryIdAsync(
                    category.UserId,
                    category.ExpenseListId,
                    cancellationToken);
            }

            var transactions = await _context.Transactions
                .Where(t => t.CategoryId == request.Id)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                transaction.CategoryId = targetCategoryId;
            }

            _context.Categories.Remove(category);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
