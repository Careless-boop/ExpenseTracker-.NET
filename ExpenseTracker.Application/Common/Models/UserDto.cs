namespace ExpenseTracker.Application.Common.Models
{
    public record UserDto(
        string Id,
        string UserName,
        string Email,
        string? DisplayName,
        string? AvatarUrl
    );
}
