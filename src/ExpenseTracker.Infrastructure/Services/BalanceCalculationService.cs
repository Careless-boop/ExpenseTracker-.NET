using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseTracker.Infrastructure.Services
{
    public class BalanceCalculationService : IBalanceCalculationService
    {
        /// <summary>Balances below this are treated as settled, so sub-cent dust isn't chased.</summary>
        private const decimal Epsilon = 0.01m;

        private readonly IApplicationDbContext _context;

        public BalanceCalculationService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ExpenseListBalances> CalculateAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default)
        {
            var members = await _context.ExpenseListMembers
                .Where(m => m.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            var transactions = await _context.ExpenseListTransactions
                .Include(t => t.Participants)
                .Where(t => t.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            var settlements = await _context.Settlements
                .Where(s => s.ExpenseListId == expenseListId)
                .ToListAsync(cancellationToken);

            var paid = members.ToDictionary(m => m.Id, _ => 0m);
            var share = members.ToDictionary(m => m.Id, _ => 0m);
            var expenseShare = members.ToDictionary(m => m.Id, _ => 0m);

            foreach (var transaction in transactions)
            {
                // No participants means nobody consumed it, so it is not a shared cost. It is
                // excluded from every per-member figure, not just from the balance.
                if (!transaction.HasSplit)
                    continue;

                // An income is money the payer *received* on the group's behalf, so it moves the
                // opposite way to an expense: the receiver holds value that belongs to the others.
                var sign = transaction.Type == TransactionType.Income ? -1m : 1m;

                if (paid.ContainsKey(transaction.PaidByMemberId))
                    paid[transaction.PaidByMemberId] += sign * transaction.Amount;

                foreach (var (memberId, memberShare) in transaction.CalculateShares())
                {
                    if (!share.ContainsKey(memberId))
                        continue;

                    share[memberId] += sign * memberShare;

                    if (transaction.Type == TransactionType.Expense)
                        expenseShare[memberId] += memberShare;
                }
            }

            var balances = members.ToDictionary(m => m.Id, m => paid[m.Id] - share[m.Id]);

            foreach (var settlement in settlements)
            {
                if (balances.ContainsKey(settlement.FromMemberId))
                    balances[settlement.FromMemberId] += settlement.Amount;

                if (balances.ContainsKey(settlement.ToMemberId))
                    balances[settlement.ToMemberId] -= settlement.Amount;
            }

            var memberBalances = members
                .Select(m => new MemberBalance(
                    m.Id,
                    m.DisplayName,
                    m.IsMock,
                    paid[m.Id],
                    share[m.Id],
                    expenseShare[m.Id],
                    balances[m.Id]))
                .ToList();

            var debts = SimplifyDebts(balances, members.ToDictionary(m => m.Id, m => m.DisplayName));

            return new ExpenseListBalances(
                memberBalances,
                debts,
                transactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount),
                transactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount));
        }

        /// <summary>
        /// Greedy min-cash-flow: repeatedly settle the largest creditor against the largest debtor.
        /// Yields at most n-1 transfers. Not provably minimal (that is NP-hard), but standard.
        /// </summary>
        private static List<DebtDto> SimplifyDebts(
            IReadOnlyDictionary<Guid, decimal> balances,
            IReadOnlyDictionary<Guid, string> displayNames)
        {
            var creditors = balances
                .Where(b => b.Value > Epsilon)
                .OrderByDescending(b => b.Value)
                .Select(b => (MemberId: b.Key, Amount: b.Value))
                .ToList();

            var debtors = balances
                .Where(b => b.Value < -Epsilon)
                .OrderBy(b => b.Value)
                .Select(b => (MemberId: b.Key, Amount: Math.Abs(b.Value)))
                .ToList();

            var debts = new List<DebtDto>();
            var creditorRemaining = creditors.Select(c => c.Amount).ToArray();
            var debtorRemaining = debtors.Select(d => d.Amount).ToArray();

            var creditorIndex = 0;
            var debtorIndex = 0;

            while (creditorIndex < creditors.Count && debtorIndex < debtors.Count)
            {
                var amount = Math.Min(creditorRemaining[creditorIndex], debtorRemaining[debtorIndex]);

                if (amount > Epsilon)
                {
                    var debtor = debtors[debtorIndex];
                    var creditor = creditors[creditorIndex];

                    debts.Add(new DebtDto(
                        debtor.MemberId,
                        displayNames.GetValueOrDefault(debtor.MemberId, "Unknown"),
                        creditor.MemberId,
                        displayNames.GetValueOrDefault(creditor.MemberId, "Unknown"),
                        Math.Round(amount, 2)));
                }

                creditorRemaining[creditorIndex] -= amount;
                debtorRemaining[debtorIndex] -= amount;

                // Both sides start above Epsilon and amount is their min, so each pass retires at
                // least one of them. The loop cannot spin.
                if (creditorRemaining[creditorIndex] <= Epsilon)
                    creditorIndex++;

                if (debtorIndex < debtors.Count && debtorRemaining[debtorIndex] <= Epsilon)
                    debtorIndex++;
            }

            return debts;
        }
    }
}
