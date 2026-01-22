using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Categories.Commands
{
    public record UpdateCategoryCommand(
        Guid Id,
        string Name,
        string? Icon,
        string? Color
    ) : IRequest;

    public class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
    {
        public UpdateCategoryCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Icon)
                .MaximumLength(50).When(x => x.Icon != null);

            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Color must be a valid hex color code");
        }
    }

    public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdateCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null)
            {
                throw new NotFoundException(nameof(Category), request.Id);
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
                    throw new ForbiddenException("You need Editor or Owner role to update categories.");
                }
            }

            category.Name = request.Name;
            category.Icon = request.Icon;
            category.Color = request.Color;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
