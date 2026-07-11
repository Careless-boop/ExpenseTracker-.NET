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
        Guid MemberId,
        string DisplayName,
        bool IsMock,
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

        public GetExpenseListBalancesQueryHandler(
            IApplicationDbContext context,
            ICurrentUserService currentUser,
            IBalanceCalculationService balanceCalculation)
        {
            _context = context;
            _currentUser = currentUser;
            _balanceCalculation = balanceCalculation;
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

            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == request.ExpenseListId)
                .ToListAsync(cancellationToken);

            var transactions = await _context.ExpenseListTransactions
                .Include(t => t.Participants)
                .Where(t => t.ExpenseListId == request.ExpenseListId)
                .ToListAsync(cancellationToken);

            var memberStats = members.ToDictionary(
                m => m.Id,
                _ => (TotalPaid: 0m, TotalShare: 0m));

            foreach (var transaction in transactions)
            {
                if (memberStats.ContainsKey(transaction.PaidByMemberId))
                {
                    var stats = memberStats[transaction.PaidByMemberId];
                    memberStats[transaction.PaidByMemberId] = (stats.TotalPaid + transaction.Amount, stats.TotalShare);
                }

                var shares = transaction.CalculateShares();
                foreach (var (memberId, share) in shares)
                {
                    if (memberStats.ContainsKey(memberId))
                    {
                        var stats = memberStats[memberId];
                        memberStats[memberId] = (stats.TotalPaid, stats.TotalShare + share);
                    }
                }
            }

            var netBalances = await _balanceCalculation.CalculateNetBalancesAsync(
                request.ExpenseListId, cancellationToken);

            var memberBalances = members
                .Select(m => new MemberBalanceDto(
                    m.Id,
                    m.DisplayName,
                    m.IsMock,
                    netBalances.GetValueOrDefault(m.Id, 0),
                    memberStats[m.Id].TotalPaid,
                    memberStats[m.Id].TotalShare
                ))
                .ToList();

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
