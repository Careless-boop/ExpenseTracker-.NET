using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Features.ExpenseLists
{
    public record ExpenseListDto(
        Guid Id,
        string Name,
        string? Description,
        string? CoverImage,
        int MemberCount,
        int TransactionCount,
        ExpenseListRole CurrentUserRole,
        DateTime CreatedAt
    );

    public record ExpenseListDetailDto(
        Guid Id,
        string Name,
        string? Description,
        string? CoverImage,
        IReadOnlyList<ExpenseListMemberDto> Members,
        int TransactionCount,
        decimal TotalExpenses,
        decimal TotalIncome,
        ExpenseListRole CurrentUserRole,
        DateTime CreatedAt
    );

    public record ExpenseListMemberDto(
        Guid MemberId,
        string DisplayName,
        string? UserId,
        string? Email,
        ExpenseListRole Role,
        DateTime JoinedAt,
        bool IsMock
    );
}
