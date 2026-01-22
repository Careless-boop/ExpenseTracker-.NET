using ExpenseTracker.Application.Common.Exceptions;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Entities;
using ExpenseTracker.Domain.Enums;
using ExpenseTracker.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Application.Features.ExpenseLists.Queries
{
    public record GetExpenseListBalancesQuery(Guid ExpenseListId) : IRequest<ExpenseListBalancesDto>;

    public record ExpenseListBalancesDto(
        Guid ExpenseListId,
        string ExpenseListName,
        IReadOnlyList<MemberBalanceDto> MemberBalances,
        IReadOnlyList<DebtDto> SimplifiedDebts,
        decimal TotalExpenses,
        decimal TotalIncome
    );

    public record MemberBalanceDto(
        string UserId,
        string? UserName,
        decimal Balance,
        decimal TotalPaid,
        decimal TotalShare
    );

    public class GetExpenseListBalancesQueryHandler
        : IRequestHandler<GetExpenseListBalancesQuery, ExpenseListBalancesDto>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IBalanceCalculationService _balanceCalculation;
        private readonly IIdentityService _identityService;

        public GetExpenseListBalancesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IBalanceCalculationService balanceCalculation,
            IIdentityService identityService)
        {
            _context = context;
            _currentUser = currentUser;
            _balanceCalculation = balanceCalculation;
            _identityService = identityService;
        }

        public async Task<ExpenseListBalancesDto> Handle(
            GetExpenseListBalancesQuery request,
            CancellationToken cancellationToken)
        {
            var membership = await _context.ExpenseListMembers
                .Include(m => m.ExpenseList)
                .FirstOrDefaultAsync(m =>
                    m.ExpenseListId == request.ExpenseListId &&
                    m.UserId == _currentUser.UserId,
                    cancellationToken);

            if (membership == null)
            {
                throw new NotFoundException(nameof(ExpenseList), request.ExpenseListId);
            }

            var expenseList = membership.ExpenseList;

            var transactions = await _context.Transactions
                .Include(t => t.Participants)
                .Where(t => t.ExpenseListId == request.ExpenseListId)
                .Where(t => !t.IsDeleted)
                .ToListAsync(cancellationToken);

            var memberIds = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.ExpenseListId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            var users = await _identityService.GetUsersByIdsAsync(memberIds);
            var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName ?? u.UserName);

            var memberStats = memberIds.ToDictionary(
                id => id,
                _ => (TotalPaid: 0m, TotalShare: 0m));

            foreach (var transaction in transactions)
            {
                if (memberStats.ContainsKey(transaction.PaidByUserId))
                {
                    var stats = memberStats[transaction.PaidByUserId];
                    memberStats[transaction.PaidByUserId] = (stats.TotalPaid + transaction.Amount, stats.TotalShare);
                }

                if (transaction.HasSplit)
                {
                    var shares = transaction.CalculateShares();
                    foreach (var (userId, share) in shares)
                    {
                        if (memberStats.ContainsKey(userId))
                        {
                            var stats = memberStats[userId];
                            memberStats[userId] = (stats.TotalPaid, stats.TotalShare + share);
                        }
                    }
                }
            }

            var netBalances = await _balanceCalculation.CalculateNetBalancesAsync(
                request.ExpenseListId, cancellationToken);

            var memberBalances = memberIds.Select(id => new MemberBalanceDto(
                id,
                userMap.GetValueOrDefault(id),
                netBalances.GetValueOrDefault(id, 0),
                memberStats[id].TotalPaid,
                memberStats[id].TotalShare
            )).ToList();

            var simplifiedDebts = await _balanceCalculation.CalculateSimplifiedDebtsAsync(
                request.ExpenseListId, cancellationToken);

            var totalExpenses = transactions
                .Where(t => t.Type == TransactionType.Expense)
                .Sum(t => t.Amount);

            var totalIncome = transactions
                .Where(t => t.Type == TransactionType.Income)
                .Sum(t => t.Amount);

            return new ExpenseListBalancesDto(
                request.ExpenseListId,
                expenseList.Name,
                memberBalances,
                simplifiedDebts,
                totalExpenses,
                totalIncome
            );
        }
    }
}
