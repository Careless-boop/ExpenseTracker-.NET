using FluentValidation;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Personal.Transactions
{
    public record GetPersonalTransactionsQuery(
        Guid? CategoryId = null,
        TransactionType? Type = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<PersonalTransactionDto>>;

    public class GetPersonalTransactionsQueryValidator : AbstractValidator<GetPersonalTransactionsQuery>
    {
        public GetPersonalTransactionsQueryValidator()
        {
            // Unbounded paging isn't just slow: PageNumber = 0 produces Skip(-20), which throws in
            // the provider and surfaces as a 500 instead of a 400.
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginatedList<PersonalTransactionDto>.MaxPageSize);
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
        }
    }

    public class GetPersonalTransactionsQueryHandler
        : IRequestHandler<GetPersonalTransactionsQuery, PaginatedList<PersonalTransactionDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetPersonalTransactionsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<PersonalTransactionDto>> Handle(
            GetPersonalTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.PersonalTransactions
                .Where(t => t.UserId == _currentUser.UserId)
                .AsQueryable();

            if (request.CategoryId.HasValue)
                query = query.Where(t => t.CategoryId == request.CategoryId.Value);

            if (request.Type.HasValue)
                query = query.Where(t => t.Type == request.Type.Value);

            if (request.FromDate.HasValue)
                query = query.Where(t => t.Date >= request.FromDate.Value.Date);

            if (request.ToDate.HasValue)
                query = query.Where(t => t.Date <= request.ToDate.Value.Date);

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new PersonalTransactionDto(
                    t.Id,
                    t.Amount,
                    t.Description,
                    t.Date,
                    t.Type,
                    t.CategoryId,
                    t.Category != null ? t.Category.Name : null,
                    t.Category != null ? t.Category.Icon : null,
                    t.Category != null ? t.Category.Color : null,
                    t.CreatedAt))
                .ToListAsync(cancellationToken);

            return new PaginatedList<PersonalTransactionDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
