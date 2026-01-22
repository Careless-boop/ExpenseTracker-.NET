using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Settlements.Queries
{
    public record GetSettlementsQuery(
        Guid ExpenseListId,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<IReadOnlyList<SettlementDto>>;

    public class GetSettlementsQueryHandler : IRequestHandler<GetSettlementsQuery, IReadOnlyList<SettlementDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public GetSettlementsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task<IReadOnlyList<SettlementDto>> Handle(
            GetSettlementsQuery request,
            CancellationToken cancellationToken)
        {
            var isMember = await _context.ExpenseListMembers
                .AnyAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (!isMember)
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            var settlements = await _context.Settlements
                .Where(s => s.ExpenseListId == request.ExpenseListId)
                .OrderByDescending(s => s.SettledAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var userIds = settlements
                .SelectMany(s => new[] { s.FromUserId, s.ToUserId })
                .Distinct();

            var users = await _identityService.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName ?? u.UserName);

            return settlements.Select(s => new SettlementDto(
                s.Id,
                s.ExpenseListId,
                s.FromUserId,
                userMap.GetValueOrDefault(s.FromUserId),
                s.ToUserId,
                userMap.GetValueOrDefault(s.ToUserId),
                s.Amount,
                s.SettledAt,
                s.Note,
                s.CreatedAt
            )).ToList();
        }
    }
}
