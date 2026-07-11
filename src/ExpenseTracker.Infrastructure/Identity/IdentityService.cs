using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExpenseTracker.Application.Common.Interfaces;
using ExpenseTracker.Application.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ExpenseTracker.Infrastructure.Identity
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IDefaultCategoryService _defaultCategoryService;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IDefaultCategoryService defaultCategoryService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _defaultCategoryService = defaultCategoryService;
        }

        public async Task<string?> GetUserNameAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user?.DisplayName ?? user?.UserName;
        }

        public async Task<UserDto?> GetUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user == null ? null : MapToDto(user);
        }

        public async Task<UserDto?> GetUserByEmailAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user == null ? null : MapToDto(user);
        }

        public async Task<IList<UserDto>> GetUsersByIdsAsync(IEnumerable<string> userIds)
        {
            var userIdList = userIds.ToList();
            var users = await _userManager.Users
                .Where(u => userIdList.Contains(u.Id))
                .ToListAsync();

            return users.Select(MapToDto).ToList();
        }

        public async Task<bool> UserExistsAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null;
        }

        public async Task<Result<string>> CreateUserAsync(string userName, string email, string password)
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                DisplayName = userName,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
            {
                return Result<string>.Failure(result.Errors.Select(e => e.Description));
            }

            await _defaultCategoryService.GetOrCreateDefaultPersonalCategoryAsync(user.Id);

            return Result<string>.Success(user.Id);
        }

        public async Task<Result> DeleteUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result.Failure("User not found");
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded
                ? Result.Success()
                : Result.Failure(result.Errors.Select(e => e.Description));
        }

        public async Task<Result<AuthResult>> AuthenticateAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result<AuthResult>.Failure("Invalid email or password");
            }

            var isValid = await _userManager.CheckPasswordAsync(user, password);
            if (!isValid)
            {
                return Result<AuthResult>.Failure("Invalid email or password");
            }

            user.LastLoginAt = DateTime.UtcNow;

            var tokens = await GenerateTokensAsync(user);

            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = tokens.ExpiresAt.AddDays(
                int.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7"));

            await _userManager.UpdateAsync(user);

            return Result<AuthResult>.Success(new AuthResult(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresAt,
                MapToDto(user)
            ));
        }

        public async Task<Result<AuthResult>> RefreshTokenAsync(string refreshToken)
        {
            var user = await _userManager.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Result<AuthResult>.Failure("Invalid or expired refresh token");
            }

            var tokens = await GenerateTokensAsync(user);

            user.RefreshToken = tokens.RefreshToken;
            user.RefreshTokenExpiryTime = tokens.ExpiresAt.AddDays(
                int.Parse(_configuration["Jwt:RefreshTokenExpiryInDays"] ?? "7"));

            await _userManager.UpdateAsync(user);

            return Result<AuthResult>.Success(new AuthResult(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresAt,
                MapToDto(user)
            ));
        }

        public async Task RevokeRefreshTokenAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);
            }
        }

        private async Task<(string AccessToken, string RefreshToken, DateTime ExpiresAt)> GenerateTokensAsync(
            ApplicationUser user)
        {
            var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryInMinutes"] ?? "60");
            var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            var refreshToken = GenerateRefreshToken();

            return (accessToken, refreshToken, expiresAt);
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private static UserDto MapToDto(ApplicationUser user) => new(
            user.Id,
            user.UserName!,
            user.Email!,
            user.DisplayName,
            user.AvatarUrl
        );
    }
}
