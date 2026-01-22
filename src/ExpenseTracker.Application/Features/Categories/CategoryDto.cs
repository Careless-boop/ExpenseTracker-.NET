using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Features.Categories
{
    public record CategoryDto(
    Guid Id,
    string Name,
    string? Icon,
    string? Color,
    bool IsDefault,
    Guid? ExpenseListId,
    int TransactionCount
);
}
