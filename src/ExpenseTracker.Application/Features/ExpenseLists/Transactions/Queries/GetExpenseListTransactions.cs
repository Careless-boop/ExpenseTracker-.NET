using FluentValidation;
using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions.Queries
{
    public record GetExpenseListTransactionsQuery(
        Guid ExpenseListId,
        Guid? CategoryId = null,
        TransactionType? Type = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<ExpenseListTransactionDto>>;

    public class GetExpenseListTransactionsQueryValidator
        : AbstractValidator<GetExpenseListTransactionsQuery>
    {
        public GetExpenseListTransactionsQueryValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginatedList<ExpenseListTransactionDto>.MaxPageSize);
            RuleFor(x => x.FromDate)
                .LessThanOrEqualTo(x => x.ToDate)
                .When(x => x.FromDate.HasValue && x.ToDate.HasValue);
        }
    }

    public class GetExpenseListTransactionsQueryHandler
        : IRequestHandler<GetExpenseListTransactionsQuery, PaginatedList<ExpenseListTransactionDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExpenseListTransactionsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<PaginatedList<ExpenseListTransactionDto>> Handle(
            GetExpenseListTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var isMember = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (!isMember)
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            var query = _context.ExpenseListTransactions
                .Where(t => t.ExpenseListId == request.ExpenseListId)
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

            var transactions = await query
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Include(t => t.PaidByMember)
                .Include(t => t.Category)
                .Include(t => t.Participants)
                    .ThenInclude(p => p.Member)
                .ToListAsync(cancellationToken);

            var items = transactions.Select(t =>
            {
                var shares = t.CalculateShares();
                var participants = t.Participants
                    .Select(p => new ExpenseListParticipantDto(
                        p.MemberId,
                        p.Member.DisplayName,
                        p.CustomShareAmount,
                        shares.GetValueOrDefault(p.MemberId, 0)
                    ))
                    .ToList();

                return new ExpenseListTransactionDto(
                    t.Id,
                    t.ExpenseListId,
                    t.Amount,
                    t.Description,
                    t.Date,
                    t.Type,
                    t.PaidByMemberId,
                    t.PaidByMember.DisplayName,
                    t.CategoryId,
                    t.Category?.Name,
                    t.Category?.Icon,
                    t.Category?.Color,
                    t.CreatedAt,
                    participants,
                    shares
                );
            }).ToList();

            return new PaginatedList<ExpenseListTransactionDto>(items, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
