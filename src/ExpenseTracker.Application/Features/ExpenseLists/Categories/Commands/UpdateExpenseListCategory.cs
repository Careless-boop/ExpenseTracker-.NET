using ExpenseTracker.Application.Common;
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
    public record UpdateExpenseListCategoryCommand(
        Guid Id,
        string Name,
        string? Icon = null,
        string? Color = null
    ) : IRequest;

    public class UpdateExpenseListCategoryCommandValidator
        : AbstractValidator<UpdateExpenseListCategoryCommand>
    {
        public UpdateExpenseListCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Icon).MaximumLength(50).When(x => x.Icon != null);
            // The column is nvarchar(7); a longer value used to pass validation and then 500 on truncation.
            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Color must be a valid hex color code (e.g., #F1F2F2)");
        }
    }

    public class UpdateExpenseListCategoryCommandHandler : IRequestHandler<UpdateExpenseListCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateExpenseListCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateExpenseListCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.ExpenseListCategories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
                throw new NotFoundException(nameof(ExpenseListCategory), request.Id);

            // Authorize before the IsDefault check, so a non-member cannot probe category ids.
            var membership = await _context.ExpenseListMembers
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == category.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null || !membership.CanEdit)
                throw new ForbiddenException("You need Editor or Owner role to manage categories.");

            await _context.EnsureNotClosedAsync(category.ExpenseListId, cancellationToken);

            if (category.IsDefault)
                throw new ValidationException([new ValidationFailure(
                    nameof(request.Id), "Cannot rename the default category")]);

            category.Name = request.Name;
            category.Icon = request.Icon;
            category.Color = request.Color;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
