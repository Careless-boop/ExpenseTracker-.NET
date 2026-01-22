using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Transactions.Queries
{
    public record GetTransactionsQuery(
        Guid? ExpenseListId = null,
        Guid? CategoryId = null,
        TransactionType? Type = null,
        DateTime? FromDate = null,
        DateTime? ToDate = null,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<TransactionDto>>;

    public class GetTransactionsQueryHandler
        : IRequestHandler<GetTransactionsQuery, PaginatedList<TransactionDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public GetTransactionsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task<PaginatedList<TransactionDto>> Handle(
            GetTransactionsQuery request,
            CancellationToken cancellationToken)
        {
            var query = _context.Transactions
                .Include(t => t.Category)
                .Include(t => t.ExpenseList)
                .Include(t => t.Participants)
                .AsQueryable();

            if (request.ExpenseListId.HasValue)
            {
                query = query
                    .Where(t => t.ExpenseListId == request.ExpenseListId.Value)
                    .Where(t => t.ExpenseList!.Members.Any(m => m.UserId == _currentUser.UserId));
            }
            else
            {
                query = query
                    .Where(t => t.CreatedByUserId == _currentUser.UserId)
                    .Where(t => t.ExpenseListId == null);
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == request.CategoryId.Value);
            }

            if (request.Type.HasValue)
            {
                query = query.Where(t => t.Type == request.Type.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(t => t.Date >= request.FromDate.Value.Date);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(t => t.Date <= request.ToDate.Value.Date);
            }

            query = query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt);

            var totalCount = await query.CountAsync(cancellationToken);

            var transactions = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = transactions
                .Select(t => t.PaidByUserId)
                .Concat(transactions.SelectMany(t => t.Participants.Select(p => p.UserId)))
                .Distinct()
                .ToList();

            var users = await _identityService.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName ?? u.UserName);

            var transactionDtos = transactions.Select(t =>
            {
                var shares = t.CalculateShares();

                var participantDtos = t.Participants.Select(p => new ParticipantDto(
                    p.UserId,
                    userMap.GetValueOrDefault(p.UserId),
                    p.CustomShareAmount,
                    shares.GetValueOrDefault(p.UserId, 0)
                )).ToList();

                return new TransactionDto(
                    t.Id,
                    t.Amount,
                    t.Description,
                    t.Date,
                    t.Type,
                    t.PaidByUserId,
                    userMap.GetValueOrDefault(t.PaidByUserId),
                    t.CategoryId,
                    t.Category.Name,
                    t.Category.Icon,
                    t.Category.Color,
                    t.ExpenseListId,
                    t.ExpenseList?.Name,
                    t.CreatedAt,
                    participantDtos.Count > 0 ? participantDtos : null,
                    shares.Count > 0 ? shares : null
                );
            }).ToList();

            return new PaginatedList<TransactionDto>(
                transactionDtos,
                totalCount,
                request.PageNumber,
                request.PageSize);
        }
    }
}
