using ExpenseTracker.Application.Common.Models;

namespace ExpenseTracker.Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<string?> GetUserNameAsync(string userId);
        Task<UserDto?> GetUserAsync(string userId);
        Task<UserDto?> GetUserByEmailAsync(string email);
        Task<IList<UserDto>> GetUsersByIdsAsync(IEnumerable<string> userIds);
        Task<bool> UserExistsAsync(string userId);
        Task<Result<string>> CreateUserAsync(string userName, string email, string password);
        Task<Result> DeleteUserAsync(string userId);
        Task<Result<AuthResult>> AuthenticateAsync(string email, string password);
        Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string userId);
    }

    public record AuthResult(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt,
        UserDto User
    );
}
