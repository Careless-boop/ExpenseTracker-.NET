using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Categories.Commands
{
    public record CreateCategoryCommand(
        string Name,
        string? Icon = null,
        string? Color = null,
        Guid? ExpenseListId = null
    ) : IRequest<Guid>;

    public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
    {
        public CreateCategoryCommandValidator()
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

    public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CreateCategoryCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<Guid> Handle(
            CreateCategoryCommand request,
            CancellationToken cancellationToken)
        {
            if (request.ExpenseListId.HasValue)
            {
                var membership = await _context.ExpenseListMembers
                    .FirstOrDefaultAsync(m =>
                        m.ExpenseListId == request.ExpenseListId.Value &&
                        m.UserId == _currentUser.UserId,
                        cancellationToken);

                if (membership == null)
                {
                    throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId.Value);
                }

                if (!membership.CanEdit)
                {
                    throw new ForbiddenException("You need Editor or Owner role to create categories.");
                }
            }

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Icon = request.Icon,
                Color = request.Color,
                IsDefault = false,
                UserId = request.ExpenseListId.HasValue ? null : _currentUser.UserId,
                ExpenseListId = request.ExpenseListId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category.Id;
        }
    }
}
