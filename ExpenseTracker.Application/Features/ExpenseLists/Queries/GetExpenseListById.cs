using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Queries
{
    public record GetExpenseListByIdQuery(Guid Id) : IRequest<ExpenseListDetailDto>;

    public class GetExpenseListByIdQueryHandler : IRequestHandler<GetExpenseListByIdQuery, ExpenseListDetailDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IIdentityService _identityService;

        public GetExpenseListByIdQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _identityService = identityService;
        }

        public async Task<ExpenseListDetailDto> Handle(
            GetExpenseListByIdQuery request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                    .ThenInclude(e => e.Members)
                .Include(m => m.ExpenseList)
                    .ThenInclude(e => e.Transactions.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.Id &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.Id);
            }

            var expenseList = membership.ExpenseList;

            var userIds = expenseList.Members.Select(m => m.UserId);
            var users = await _identityService.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.Id);

            var memberDtos = expenseList.Members
                .Select(m => {
                    var user = userMap.GetValueOrDefault(m.UserId);
                    return new ExpenseListMemberDto(
                        m.UserId,
                        user?.DisplayName ?? user?.UserName,
                        user?.Email,
                        user?.AvatarUrl,
                        m.Role,
                        m.JoinedAt
                    );
                })
                .OrderByDescending(m => m.Role)
                .ThenBy(m => m.JoinedAt)
                .ToList();

            var totalExpenses = expenseList.Transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var totalIncome = expenseList.Transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            return new ExpenseListDetailDto(
                expenseList.Id,
                expenseList.Name,
                expenseList.Description,
                expenseList.CoverImage,
                memberDtos,
                expenseList.Transactions.Count,
                totalExpenses,
                totalIncome,
                membership.Role,
                expenseList.CreatedAt
            );
        }
    }
}
