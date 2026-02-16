using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JwtAuth.Shared.Options
{
    public sealed class RefreshTokenSettings
    {
        public int DaysToExpire { get; set; } = 17;
    }
}
