using ExpenseTracker.Application.Common;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Commands
{
    public record CreateExpenseListCommand(
        string Name,
        string? Description = null,
        string? CoverImage = null,
        // Null falls back to the creator's currency setting.
        string? Currency = null
    ) : IRequest<Guid>;

    public class CreateExpenseListCommandValidator : AbstractValidator<CreateExpenseListCommand>
    {
        public CreateExpenseListCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

            RuleFor(x => x.Description)
                .MaximumLength(1000).When(x => x.Description != null);

            RuleFor(x => x.CoverImage)
                .MaximumLength(500).When(x => x.CoverImage != null);

            RuleFor(x => x.Currency)
                .Must(SupportedCurrencies.IsSupported)
                .When(x => x.Currency != null)
                .WithMessage("Unsupported currency.");
        }
    }

    public class CreateExpenseListCommandHandler : IRequestHandler<CreateExpenseListCommand, Guid>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public CreateExpenseListCommandHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService,
            IDefaultCategoryService defaultCategoryService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task<Guid> Handle(
            CreateExpenseListCommand request,
            CancellationToken cancellationToken)
        {
            var currentUserId = _currentUser.UserId!;
            var user = await _identityService.GetUserAsync(currentUserId);

            var currency = request.Currency
                ?? await _context.UserSettings
                    .Where(s => s.UserId == currentUserId)
                    .Select(s => s.Currency)
                    .FirstOrDefaultAsync(cancellationToken)
                ?? SupportedCurrencies.Default;

            var expenseList = new ExpenseList
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                CoverImage = request.CoverImage,
                Currency = SupportedCurrencies.Normalize(currency)
            };

            expenseList.Members.Add(new ExpenseListMember
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                DisplayName = user?.DisplayName ?? user?.UserName ?? currentUserId,
                Email = user?.Email,
                Role = ExpenseListRole.Owner,
                JoinedAt = DateTime.UtcNow
            });

            _context.ExpenseLists.Add(expenseList);
            await _context.SaveChangesAsync(cancellationToken);

            await _defaultCategoryService.GetOrCreateDefaultExpenseListCategoryAsync(
                expenseList.Id, cancellationToken);

            return expenseList.Id;
        }
    }
}
