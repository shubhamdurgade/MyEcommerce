using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JwtAuth.Shared.Options
{
    public sealed class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string SigningKey { get; set; } = string.Empty;

        public int AccessTokenMinutes { get; set; } = 15;
    }
}
