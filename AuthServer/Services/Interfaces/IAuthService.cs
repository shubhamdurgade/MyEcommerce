using AuthServer.DTOs.Requests;
using AuthServer.DTOs.Responses;
namespace AuthServer.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequestDTO dto);

        Task<TokenResponseDTO> LoginAsync(LoginRequestDTO Dto, HttpContext httpContext);

        Task<TokenResponseDTO> RefreshAsync(RefreshRequestDTO dto, HttpContext httpContext);

        Task LogoutAsync(LogoutRequestDTO dto);

        Task<List<SessionResponseDTO>> GetMySessionAsync(Guid userId);

        Task RevokeSessionAsync(Guid userId, Guid sessionId, string reason);
    }
}
