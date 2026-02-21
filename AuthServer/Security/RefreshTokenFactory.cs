using JwtAuth.Shared.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace AuthServer.Security
{
    public sealed class RefreshTokenService : IRefreshTokenService
    {
        private readonly RefreshTokenSettings _refreshSettings;

        public RefreshTokenService(IOptions<RefreshTokenSettings> refreshOptions)
        {
            _refreshSettings = refreshOptions.Value;
        }
        public string GenerateRawToken(int byteLength = 64)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return ToBase64Url(bytes);
        }

        public DateTime GetExpiryUtc()
        {
            var days = _refreshSettings.DaysToExpire <= 0 ? 14 : _refreshSettings.DaysToExpire;
            return DateTime.UtcNow.AddDays(days);
        }

        public string HashToken(string rawToken)
        {
            var bytes = Encoding.UTF8.GetBytes(rawToken);

            var hash = SHA256.HashData(bytes);

            return Convert.ToHexString(hash);
        }

        private static string ToBase64Url(byte[] bytes)
        {
            var base64 = Convert.ToBase64String(bytes);
            return base64.TrimEnd('=').Replace("+", "-").Replace("/", "_");
        }
    }
}
