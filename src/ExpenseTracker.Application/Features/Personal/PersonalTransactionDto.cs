using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Features.Personal
{
    public record PersonalTransactionDto(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid? CategoryId,
        string? CategoryName,
        string? CategoryIcon,
        string? CategoryColor,
        DateTime CreatedAt
    );
}
