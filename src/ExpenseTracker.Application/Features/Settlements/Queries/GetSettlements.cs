using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.Settlements.Queries
{
    public record GetSettlementsQuery(
        Guid ExpenseListId,
        int PageNumber = 1,
        int PageSize = 20
    ) : IRequest<PaginatedList<SettlementDto>>;

    public class GetSettlementsQueryValidator : AbstractValidator<GetSettlementsQuery>
    {
        public GetSettlementsQueryValidator()
        {
            RuleFor(x => x.ExpenseListId).NotEmpty();
            RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, PaginatedList<SettlementDto>.MaxPageSize);
        }
    }

    public class GetSettlementsQueryHandler
        : IRequestHandler<GetSettlementsQuery, PaginatedList<SettlementDto>>
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

        public async Task<PaginatedList<SettlementDto>> Handle(
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

            var query = _context.Settlements
                .Where(s => s.ExpenseListId == request.ExpenseListId)
                .OrderByDescending(s => s.SettledAt)
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
                ));

            return await PaginatedList<SettlementDto>.CreateAsync(
                query, request.PageNumber, request.PageSize, cancellationToken);
        }
    }
}
