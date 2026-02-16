using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JwtAuth.Shared.Options
{
    public sealed class JwtAuthSharedOptions
    {
        public bool RequireHttpsMetadata { get; set; } = false;

        public bool SaveToken { get; set; } = true;

        public int ClockSkewMinutes { get; set; } = 30;

        public bool AddsharedPolicies { get; set; } = true;

        public bool EnableAuthFailureLogging { get; set; } = true;

        public bool EnableAuthFailreLogging { get; set; } = true;

        public string LoggerCategoryName { get; set; } = "JwtBearerAuth";

        public JsonSerializerOptions JsonSerializerOptions { get; set; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = null
        };
    }
}
