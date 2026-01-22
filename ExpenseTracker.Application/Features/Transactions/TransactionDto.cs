using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Features.Transactions
{
    public record TransactionDto(
        Guid Id,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        string PaidByUserId,
        string? PaidByUserName,
        Guid CategoryId,
        string CategoryName,
        string? CategoryIcon,
        string? CategoryColor,
        Guid? ExpenseListId,
        string? ExpenseListName,
        DateTime CreatedAt,
        IReadOnlyList<ParticipantDto>? Participants = null,
        IReadOnlyDictionary<string, decimal>? CalculatedShares = null
    );

    public record ParticipantDto(
        string UserId,
        string? UserName,
        decimal? CustomShareAmount,
        decimal CalculatedShare
    );
}
