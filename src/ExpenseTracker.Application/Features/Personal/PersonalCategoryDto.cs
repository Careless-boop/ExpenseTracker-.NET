namespace ExpenseTracker.Application.Features.Personal
{
    public record PersonalCategoryDto(
        Guid Id,
        string Name,
        string? Icon,
        string? Color,
        bool IsDefault,
        int TransactionCount
    );
}
