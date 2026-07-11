namespace ExpenseTracker.Application.Features.ExpenseLists.Categories
{
    public record ExpenseListCategoryDto(
        Guid Id,
        Guid ExpenseListId,
        string Name,
        string? Icon,
        string? Color,
        bool IsDefault,
        int TransactionCount
    );
}
