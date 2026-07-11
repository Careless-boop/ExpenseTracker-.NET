using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Categories
{
    public record DeletePersonalCategoryCommand(
        Guid Id,
        Guid? ReassignToCategoryId = null
    ) : IRequest;

    public class DeletePersonalCategoryCommandValidator : AbstractValidator<DeletePersonalCategoryCommand>
    {
        public DeletePersonalCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }

    public class DeletePersonalCategoryCommandHandler : IRequestHandler<DeletePersonalCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public DeletePersonalCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task Handle(
            DeletePersonalCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.PersonalCategories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null || category.UserId != _currentUser.UserId)
                throw new NotFoundException(nameof(PersonalCategory), request.Id);

            if (category.IsDefault)
                throw new ForbiddenException("Default category cannot be deleted.");

            Guid targetId;
            if (request.ReassignToCategoryId.HasValue)
            {
                var target = await _context.PersonalCategories
                    .FirstOrDefaultAsync(c => c.Id == request.ReassignToCategoryId.Value, cancellationToken);

                if (target == null || target.UserId != _currentUser.UserId)
                    throw new NotFoundException(nameof(PersonalCategory), request.ReassignToCategoryId.Value);

                targetId = target.Id;
            }
            else
            {
                targetId = await _defaultCategoryService.GetOrCreateDefaultPersonalCategoryAsync(
                    _currentUser.UserId!, cancellationToken);
            }

            var transactions = await _context.PersonalTransactions
                .Where(t => t.CategoryId == request.Id)
                .ToListAsync(cancellationToken);

            foreach (var t in transactions)
                t.CategoryId = targetId;

            _context.PersonalCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
