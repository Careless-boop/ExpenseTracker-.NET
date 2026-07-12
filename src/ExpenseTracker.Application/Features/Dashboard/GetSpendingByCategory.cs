using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Dashboard
{
    public record GetSpendingByCategoryQuery(
        DateTime? From = null,
        DateTime? To = null,
        TransactionType Type = TransactionType.Expense
    ) : IRequest<CategoryBreakdownDto>;

    public class GetSpendingByCategoryQueryValidator : AbstractValidator<GetSpendingByCategoryQuery>
    {
        public GetSpendingByCategoryQueryValidator()
        {
            RuleFor(x => x.Type).IsInEnum();
            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.To)
                .When(x => x.From.HasValue && x.To.HasValue)
                .WithMessage("'from' must not be after 'to'.");
        }
    }

    public class GetSpendingByCategoryQueryHandler
        : IRequestHandler<GetSpendingByCategoryQuery, CategoryBreakdownDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetSpendingByCategoryQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<CategoryBreakdownDto> Handle(
            GetSpendingByCategoryQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId!;
            var period = DashboardPeriod.Resolve(request.From, request.To);

            var groups = await _context.PersonalTransactions
                .Where(t =>
                    t.UserId == userId &&
                    t.Type == request.Type &&
                    t.CategoryId != null &&
                    t.Date >= period.From &&
                    t.Date <= period.To)
                .GroupBy(t => new
                {
                    CategoryId = t.CategoryId!.Value,
                    t.Category!.Name,
                    t.Category.Icon,
                    t.Category.Color
                })
                .Select(g => new
                {
                    g.Key.CategoryId,
                    g.Key.Name,
                    g.Key.Icon,
                    g.Key.Color,
                    Total = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Total)
                .ToListAsync(cancellationToken);

            var total = groups.Sum(g => g.Total);

            var categories = groups
                .Select(g => new CategoryBreakdownItemDto(
                    g.CategoryId,
                    g.Name,
                    g.Icon,
                    g.Color,
                    g.Total,
                    total == 0 ? 0 : Math.Round(g.Total / total * 100, 2),
                    g.Count))
                .ToList();

            return new CategoryBreakdownDto(period.From, period.To, total, categories);
        }
    }
}
