using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Features.ExpenseLists.Transactions
{
    public record ExpenseListTransactionDto(
        Guid Id,
        Guid ExpenseListId,
        decimal Amount,
        string? Description,
        DateTime Date,
        TransactionType Type,
        Guid PaidByMemberId,
        string PaidByDisplayName,
        Guid? CategoryId,
        string? CategoryName,
        string? CategoryIcon,
        string? CategoryColor,
        DateTime CreatedAt,
        IReadOnlyList<ExpenseListParticipantDto> Participants,
        IReadOnlyDictionary<Guid, decimal> CalculatedShares
    );

    public record ExpenseListParticipantDto(
        Guid MemberId,
        string DisplayName,
        decimal? CustomShareAmount,
        decimal CalculatedShare
    );
}
