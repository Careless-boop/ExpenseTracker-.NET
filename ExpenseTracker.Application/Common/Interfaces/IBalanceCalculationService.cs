namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IBalanceCalculationService
    {
        /// <summary>
        /// Calculate net balances for all members in an expense list.
        /// Positive = is owed money, Negative = owes money
        /// </summary>
        Task<Dictionary<string, decimal>> CalculateNetBalancesAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate simplified debts (minimum number of transactions to settle all debts)
        /// </summary>
        Task<IReadOnlyList<DebtDto>> CalculateSimplifiedDebtsAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default);
    }

    public record DebtDto(
        string FromUserId,
        string? FromUserName,
        string ToUserId,
        string? ToUserName,
        decimal Amount
    );
}
