using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Categories.Commands
{
    public record CreateExpenseListCategoryCommand(
        Guid ExpenseListId,
        string Name,
        string? Icon = null,
        string? Color = null
    ) : IRequest<Guid>;

    public class CreateExpenseListCategoryCommandValidator
        : AbstractValidator<CreateExpenseListCategoryCommand>
    {
        public CreateExpenseListCategoryCommandValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Icon).MaximumLength(50).When(x => x.Icon != null);
            // The column is nvarchar(7); a longer value used to pass validation and then 500 on truncation.
            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Color must be a valid hex color code (e.g., #F1F2F2)");
        }
    }

    public class CreateExpenseListCategoryCommandHandler
        : IRequestHandler<CreateExpenseListCategoryCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateExpenseListCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateExpenseListCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            if (!membership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to manage categories.");

            await _context.EnsureNotClosedAsync(request.ExpenseListId, cancellationToken);

            var category = new ExpenseListCategory
            {
                Id = Guid.NewGuid(),
                ExpenseListId = request.ExpenseListId,
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                IsDefault = false
            };

            _context.ExpenseListCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }
}
