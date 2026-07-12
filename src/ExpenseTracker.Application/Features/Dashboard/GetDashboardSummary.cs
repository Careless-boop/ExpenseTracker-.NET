using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Dashboard
{
    public record GetDashboardSummaryQuery(
        DateTime? From = null,
        DateTime? To = null
    ) : IRequest<DashboardSummaryDto>;

    public class GetDashboardSummaryQueryValidator : AbstractValidator<GetDashboardSummaryQuery>
    {
        public GetDashboardSummaryQueryValidator()
        {
            RuleFor(x => x.From)
                .LessThanOrEqualTo(x => x.To)
                .When(x => x.From.HasValue && x.To.HasValue)
                .WithMessage("'from' must not be after 'to'.");
        }
    }

    public class GetDashboardSummaryQueryHandler
        : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetDashboardSummaryQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<DashboardSummaryDto> Handle(
            GetDashboardSummaryQuery request,
            CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId!;
            var period = DashboardPeriod.Resolve(request.From, request.To);
            var previous = period.Previous();

            var current = await TotalsForAsync(userId, period, cancellationToken);
            var prior = await TotalsForAsync(userId, previous, cancellationToken);

            decimal? netChange = prior.Net == 0
                ? null
                : Math.Round((current.Net - prior.Net) / Math.Abs(prior.Net) * 100, 2);

            return new DashboardSummaryDto(period.From, period.To, current, prior, netChange);
        }

        private async Task<PeriodTotalsDto> TotalsForAsync(
            string userId,
            DashboardPeriod period,
            CancellationToken cancellationToken)
        {
            // Grouped in the database rather than materialising the user's transactions.
            var byType = await _context.PersonalTransactions
                .Where(t =>
                    t.UserId == userId &&
                    t.Date >= period.From &&
                    t.Date <= period.To)
                .GroupBy(t => t.Type)
                .Select(g => new
                {
                    Type = g.Key,
                    Total = g.Sum(t => t.Amount),
                    Count = g.Count()
                })
                .ToListAsync(cancellationToken);

            var income = byType.FirstOrDefault(t => t.Type == TransactionType.Income);
            var expense = byType.FirstOrDefault(t => t.Type == TransactionType.Expense);

            var totalIncome = income?.Total ?? 0m;
            var totalExpenses = expense?.Total ?? 0m;

            return new PeriodTotalsDto(
                totalIncome,
                totalExpenses,
                totalIncome - totalExpenses,
                (income?.Count ?? 0) + (expense?.Count ?? 0));
        }
    }
}
