namespace AuthServer.DTOs.Responses
{
    public class TokenResponseDTO
    {
        public string AccesToken { get; set; } = default!;

        public DateTime AccessTokenExperisUtc { get; set; }

        public string RefreshToken { get; set; } = default!;

        public DateTime RefreshTokenExperisUtcUtc { get; set; } 

        public Guid SessionId { get; set; }
    }
}
