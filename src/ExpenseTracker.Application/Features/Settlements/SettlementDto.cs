namespace ExpenseTracker.Application.Features.Settlements
{
    public record SettlementDto(
        Guid Id,
        Guid ExpenseListId,
        string FromUserId,
        string? FromUserName,
        string ToUserId,
        string? ToUserName,
        decimal Amount,
        DateTime SettledAt,
        string? Note,
        DateTime CreatedAt
    );
}
