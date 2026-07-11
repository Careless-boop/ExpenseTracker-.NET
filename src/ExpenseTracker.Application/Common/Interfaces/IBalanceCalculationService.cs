namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IBalanceCalculationService
    {
        /// <summary>
        /// Computes every member's position in a single pass, plus the transfers that settle them.
        /// Positive balance = is owed money, negative = owes money. Balances always sum to zero.
        /// </summary>
        Task<ExpenseListBalances> CalculateAsync(
            Guid expenseListId,
            CancellationToken cancellationToken = default);
    }

    public record ExpenseListBalances(
        IReadOnlyList<MemberBalance> Members,
        IReadOnlyList<DebtDto> SimplifiedDebts,
        decimal TotalExpenses,
        decimal TotalIncome
    );

    /// <summary>
    /// TotalPaid and TotalShare are signed by transaction type (income inverts), so that
    /// Balance == TotalPaid - TotalShare always holds. TotalExpenseShare is the unsigned
    /// expense-only share — what this member actually consumed.
    /// </summary>
    public record MemberBalance(
        Guid MemberId,
        string DisplayName,
        bool IsMock,
        decimal TotalPaid,
        decimal TotalShare,
        decimal TotalExpenseShare,
        decimal Balance
    );

    public record DebtDto(
        Guid FromMemberId,
        string FromDisplayName,
        Guid ToMemberId,
        string ToDisplayName,
        decimal Amount
    );
}
