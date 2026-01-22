using ExpenseTracker.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services
{
    public class BalanceCalculationService : IBalanceCalculationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IIdentityService _identityService;

        public BalanceCalculationService(
            IApplicationDbContext context,
            IIdentityService identityService)
        {
            _context = context;
            _identityService = identityService;
        }

        public async Task<Dictionary<string, decimal>> CalculateNetBalancesAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var balances = new Dictionary<string, decimal>();

            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == expenseListId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            foreach (var memberId in members)
            {
                balances[memberId] = 0;
            }

            var transactions = await _context.Transactions
                .Include(t => t.Participants)
                .Where(t => t.ExpenseListId == expenseListId)
                .Where(t => !t.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                if (!transaction.HasSplit)
                {
                    continue;
                }

                var shares = transaction.CalculateShares();
                var paidBy = transaction.PaidByUserId;

                if (balances.ContainsKey(paidBy))
                {
                    balances[paidBy] += transaction.Amount;
                }

                foreach (var (userId, share) in shares)
                {
                    if (balances.ContainsKey(userId))
                    {
                        balances[userId] -= share;
                    }
                }
            }

            var settlements = await _context.Settlements
                .Where(s => s.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            foreach (var settlement in settlements)
            {
                if (balances.ContainsKey(settlement.FromUserId))
                {
                    balances[settlement.FromUserId] += settlement.Amount; 
                }
                if (balances.ContainsKey(settlement.ToUserId))
                {
                    balances[settlement.ToUserId] -= settlement.Amount;
                }
            }

            return balances;
        }

        public async Task<IReadOnlyList<DebtDto>> CalculateSimplifiedDebtsAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var balances = await CalculateNetBalancesAsync(expenseListId, cancellationToken);

            var creditors = balances
                .Where(b => b.Value > 0.01m)
                .OrderByDescending(b => b.Value)
                .Select(b => new { UserId = b.Key, Amount = b.Value })
                .ToList();

            var debtors = balances
                .Where(b => b.Value < -0.01m)
                .OrderBy(b => b.Value)
                .Select(b => new { UserId = b.Key, Amount = Math.Abs(b.Value) })
                .ToList();

            var debts = new List<DebtDto>();

            var userIds = creditors.Select(c => c.UserId)
                .Concat(debtors.Select(d => d.UserId))
                .Distinct();
            var users = await _identityService.GetUsersByIdsAsync(userIds);
            var userMap = users.ToDictionary(u => u.Id, u => u.DisplayName ?? u.UserName);

            var creditorAmounts = creditors.Select(c => c.Amount).ToArray();
            var debtorAmounts = debtors.Select(d => d.Amount).ToArray();

            var creditorIndex = 0;
            var debtorIndex = 0;

            while (creditorIndex < creditors.Count && debtorIndex < debtors.Count)
            {
                var creditor = creditors[creditorIndex];
                var debtor = debtors[debtorIndex];

                var amount = Math.Min(creditorAmounts[creditorIndex], debtorAmounts[debtorIndex]);

                if (amount > 0.01m)
                {
                    debts.Add(new DebtDto(
                        debtor.UserId,
                        userMap.GetValueOrDefault(debtor.UserId),
                        creditor.UserId,
                        userMap.GetValueOrDefault(creditor.UserId),
                        Math.Round(amount, 2)
                    ));
                }

                creditorAmounts[creditorIndex] -= amount;
                debtorAmounts[debtorIndex] -= amount;

                if (creditorAmounts[creditorIndex] < 0.01m)
                {
                    creditorIndex++;
                }

                if (debtorAmounts[debtorIndex] < 0.01m)
                {
                    debtorIndex++;
                }
            }

            return debts;
        }
    }
}
