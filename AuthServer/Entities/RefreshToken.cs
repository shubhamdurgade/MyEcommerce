using System.ComponentModel.DataAnnotations;

namespace AuthServer.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid SessionId { get;set; }

        public UserSession Session { get; set; } = default!;

        [MaxLength(200)]
        public string TokenHash { get; set; } = default!;

        public Guid? ParentTokenId { get; set; }

        public RefreshToken? ParentToken { get; set; }

        public Guid? ReplaceByTokenId { get; set; }

        public RefreshToken? ReplacedByToken { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
         
        public DateTime ExpiresUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedUtc { get; set; }

        public string? RevokedReason { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;

        public bool IsRevoked => RevokedUtc != null;

        public bool IsActive => !IsExpired && !IsRevoked; 
    }
}
