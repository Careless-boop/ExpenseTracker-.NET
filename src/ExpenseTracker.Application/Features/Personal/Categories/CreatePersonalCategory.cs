using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;

namespace ExpenseTracker.Application.Features.Personal.Categories
{
    public record CreatePersonalCategoryCommand(
        string Name,
        string? Icon = null,
        string? Color = null
    ) : IRequest<Guid>;

    public class CreatePersonalCategoryCommandValidator : AbstractValidator<CreatePersonalCategoryCommand>
    {
        public CreatePersonalCategoryCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

            RuleFor(x => x.Icon)
                .MaximumLength(50).When(x => x.Icon != null);

            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Color must be a valid hex color code (e.g., #F1F2F2)");
        }
    }

    public class CreatePersonalCategoryCommandHandler : IRequestHandler<CreatePersonalCategoryCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreatePersonalCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreatePersonalCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = new PersonalCategory
            {
                Id = Guid.NewGuid(),
                UserId = _currentUser.UserId!,
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                IsDefault = false
            };

            _context.PersonalCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }
}
