using System.Text;
using Microsoft.Extensions.Configuration;

namespace ExpenseTracker.Infrastructure.Identity
{
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Key { get; init; } = null!;
        public string Issuer { get; init; } = null!;
        public string Audience { get; init; } = null!;
        public int ExpiryInMinutes { get; init; } = 60;
        public int RefreshTokenExpiryInDays { get; init; } = 7;

        /// <summary>
        /// Binds and validates the Jwt section, failing at startup rather than letting a missing
        /// key surface as an ArgumentNullException on the first request.
        /// </summary>
        public static JwtSettings Load(IConfiguration configuration)
        {
            const string howToConfigure =
                "Set it with `dotnet user-secrets set \"Jwt:Key\" \"<value>\"` for local development, " +
                "or the Jwt__Key environment variable when deploying.";

            var settings = configuration.GetSection(SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    $"Configuration section '{SectionName}' is missing. {howToConfigure}");

            if (string.IsNullOrWhiteSpace(settings.Key))
                throw new InvalidOperationException($"Jwt:Key is not configured. {howToConfigure}");

            // HMAC-SHA256 requires at least 256 bits.
            if (Encoding.UTF8.GetByteCount(settings.Key) < 32)
                throw new InvalidOperationException(
                    "Jwt:Key must be at least 32 bytes (256 bits) to sign with HMAC-SHA256.");

            if (string.IsNullOrWhiteSpace(settings.Issuer))
                throw new InvalidOperationException("Jwt:Issuer is not configured.");

            if (string.IsNullOrWhiteSpace(settings.Audience))
                throw new InvalidOperationException("Jwt:Audience is not configured.");

            if (settings.ExpiryInMinutes <= 0)
                throw new InvalidOperationException("Jwt:ExpiryInMinutes must be greater than zero.");

            if (settings.RefreshTokenExpiryInDays <= 0)
                throw new InvalidOperationException("Jwt:RefreshTokenExpiryInDays must be greater than zero.");

            return settings;
        }
    }
}
