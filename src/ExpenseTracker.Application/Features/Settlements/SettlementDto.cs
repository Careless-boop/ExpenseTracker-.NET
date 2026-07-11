namespace ExpenseTracker.Application.Features.Settlements
{
    public record SettlementDto(
        Guid Id,
        Guid ExpenseListId,
        Guid FromMemberId,
        string FromDisplayName,
        Guid ToMemberId,
        string ToDisplayName,
        decimal Amount,
        DateTime SettledAt,
        string? Note,
        DateTime CreatedAt
    );
}
