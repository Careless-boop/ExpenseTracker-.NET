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

        public GetSettlementsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
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
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);

            return await _context.Settlements
                .Where(s => s.ExpenseListId == request.ExpenseListId)
                .OrderByDescending(s => s.SettledAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(s => new SettlementDto(
                    s.Id,
                    s.ExpenseListId,
                    s.FromMemberId,
                    s.FromMember.DisplayName,
                    s.ToMemberId,
                    s.ToMember.DisplayName,
                    s.Amount,
                    s.SettledAt,
                    s.Note,
                    s.CreatedAt
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
