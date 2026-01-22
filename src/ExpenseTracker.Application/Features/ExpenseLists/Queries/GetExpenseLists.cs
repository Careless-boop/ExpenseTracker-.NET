using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Queries
{
    public record GetExpenseListsQuery : IRequest<IReadOnlyList<ExpenseListDto>>;

    public class GetExpenseListsQueryHandler : IRequestHandler<GetExpenseListsQuery, IReadOnlyList<ExpenseListDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public GetExpenseListsQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<ExpenseListDto>> Handle(
            GetExpenseListsQuery request,
            CancellationToken cancellationToken)
        {
            return await _context.ExpenseListMembers
                .Where(m => m.UserId == _currentUser.UserId)
                .Select(m => new ExpenseListDto(
                    m.ExpenseList.Id,
                    m.ExpenseList.Name,
                    m.ExpenseList.Description,
                    m.ExpenseList.CoverImage,
                    m.ExpenseList.Members.Count,
                    m.ExpenseList.Transactions.Count(t => !t.IsDeleted),
                    m.Role,
                    m.ExpenseList.CreatedAt
                ))
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
