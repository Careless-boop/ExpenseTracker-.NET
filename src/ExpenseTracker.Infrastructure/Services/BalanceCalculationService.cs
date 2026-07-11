using ExpenseTracker.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services
{
    public class BalanceCalculationService : IBalanceCalculationService
    {
        private readonly IApplicationDbContext _context;

        public BalanceCalculationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Dictionary<Guid, decimal>> CalculateNetBalancesAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            var balances = members.ToDictionary(m => m.Id, _ => 0m);

            var transactions = await _context.ExpenseListTransactions
                .Include(t => t.Participants)
                .Where(t => t.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            foreach (var transaction in transactions)
            {
                if (!transaction.HasSplit)
                    continue;

                var shares = transaction.CalculateShares();

                if (balances.ContainsKey(transaction.PaidByMemberId))
                    balances[transaction.PaidByMemberId] += transaction.Amount;

                foreach (var (memberId, share) in shares)
                {
                    if (balances.ContainsKey(memberId))
                        balances[memberId] -= share;
                }
            }

            var settlements = await _context.Settlements
                .Where(s => s.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            foreach (var settlement in settlements)
            {
                if (balances.ContainsKey(settlement.FromMemberId))
                    balances[settlement.FromMemberId] += settlement.Amount;

                if (balances.ContainsKey(settlement.ToMemberId))
                    balances[settlement.ToMemberId] -= settlement.Amount;
            }

            return balances;
        }

        public async Task<IReadOnlyList<DebtDto>> CalculateSimplifiedDebtsAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == expenseListId)
                .ToDictionaryAsync(m => m.Id, m => m.DisplayName, cancellationToken);

            var balances = await CalculateNetBalancesAsync(expenseListId, cancellationToken);

            var creditors = balances
                .Where(b => b.Value > 0.01m)
                .OrderByDescending(b => b.Value)
                .Select(b => new { MemberId = b.Key, Amount = b.Value })
                .ToList();

            var debtors = balances
                .Where(b => b.Value < -0.01m)
                .OrderBy(b => b.Value)
                .Select(b => new { MemberId = b.Key, Amount = Math.Abs(b.Value) })
                .ToList();

            var debts = new List<DebtDto>();
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
                        debtor.MemberId,
                        members.GetValueOrDefault(debtor.MemberId, "Unknown"),
                        creditor.MemberId,
                        members.GetValueOrDefault(creditor.MemberId, "Unknown"),
                        Math.Round(amount, 2)
                    ));
                }

                creditorAmounts[creditorIndex] -= amount;
                debtorAmounts[debtorIndex] -= amount;

                if (creditorAmounts[creditorIndex] < 0.01m)
                    creditorIndex++;

                if (debtorAmounts[debtorIndex] < 0.01m)
                    debtorIndex++;
            }

            return debts;
        }
    }
}
