namespace ExpenseTracker.Application.Features.Dashboard
{
    public record PeriodTotalsDto(
        decimal TotalIncome,
        decimal TotalExpenses,
        decimal Net,
        int TransactionCount
    );

    /// <param name="Previous">The immediately preceding window of equal length.</param>
    /// <param name="NetChangePercent">Null when the previous net was zero, since the change is undefined.</param>
    public record DashboardSummaryDto(
        DateTime From,
        DateTime To,
        PeriodTotalsDto Current,
        PeriodTotalsDto Previous,
        decimal? NetChangePercent
    );

    public record CategoryBreakdownItemDto(
        Guid CategoryId,
        string Name,
        string? Icon,
        string? Color,
        decimal Total,
        decimal Percentage,
        int TransactionCount
    );

    public record CategoryBreakdownDto(
        DateTime From,
        DateTime To,
        decimal Total,
        IReadOnlyList<CategoryBreakdownItemDto> Categories
    );
}
