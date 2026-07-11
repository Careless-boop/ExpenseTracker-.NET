using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Categories
{
    public record UpdatePersonalCategoryCommand(
        Guid Id,
        string Name,
        string? Icon,
        string? Color
    ) : IRequest;

    public class UpdatePersonalCategoryCommandValidator : AbstractValidator<UpdatePersonalCategoryCommand>
    {
        public UpdatePersonalCategoryCommandValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters");
            RuleFor(x => x.Icon).MaximumLength(50).When(x => x.Icon != null);
            RuleFor(x => x.Color)
                .Matches(@"^#[0-9A-Fa-f]{6}$")
                .When(x => !string.IsNullOrEmpty(x.Color))
                .WithMessage("Color must be a valid hex color code");
        }
    }

    public class UpdatePersonalCategoryCommandHandler : IRequestHandler<UpdatePersonalCategoryCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public UpdatePersonalCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task Handle(
            UpdatePersonalCategoryCommand request,
            CancellationToken cancellationToken)
        {
            var category = await _context.PersonalCategories
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

            if (category == null || category.UserId != _currentUser.UserId)
                throw new NotFoundException(nameof(PersonalCategory), request.Id);

            category.Name = request.Name;
            category.Icon = request.Icon;
            category.Color = request.Color;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
