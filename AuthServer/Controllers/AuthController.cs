using AuthServer.Common.Exceptions;
using AuthServer.Common.Results;
using AuthServer.DTOs.Requests;
using AuthServer.DTOs.Responses;
using AuthServer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuthServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService=authService;
            _logger=logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<string>>> Register([FromBody] RegisterRequestDTO dto)
        {
            _logger.LogInformation("Register request received. Email={Email}, FirstName={FirstName}, LastName{LastName}, PhoneNumber{PhoneNuber}",
                dto.Email, dto.FirstName, dto.LastName, dto.PhoneNumber);

            await _authService.RegisterAsync(dto);

            _logger.LogInformation("Register completed. Email={Email}", dto.Email);

            return Ok(ApiResponse<string>.Success("User registerd succesfully"));

        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<TokenResponseDTO>>> Login([FromBody] LoginRequestDTO dto)
        {
            _logger.LogInformation("Login request received. Email = {Email}, CLientId={ClientId}, DeviceId={DeviceId}", dto.Email, dto.ClientId, dto.DeviceId);

            var result = await _authService.LoginAsync(dto, HttpContext);

            _logger.LogInformation("Login Completed. Email = {Email}, CLientId={ClientId}, SessionId={SessionId}", dto.Email, dto.ClientId, result.SessionId);

            return Ok(ApiResponse<TokenResponseDTO>.Success(result, "Login successful."));
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<TokenResponseDTO>>> Refresh([FromBody] RefreshRequestDTO dto)
        {
            _logger.LogInformation("Refresh request recevied. ClientId={ClientId}, DeviceId={DeviceId}", dto.ClientId, dto.DeviceId);

            var result = await _authService.RefreshAsync(dto, HttpContext);

            _logger.LogInformation("Refresh completed. Client={ClientId}, Session={SessionId}",dto.ClientId, result.SessionId);

            return Ok(ApiResponse<TokenResponseDTO>.Success(result, "Token refreshed succesfully.")); 

        }

        [HttpPost("Logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> Logout([FromBody] LogoutRequestDTO dto)
        {
            var userId = GetUserId();

            _logger.LogInformation("Logout request received. UserId={UserID}, Client={Client}, DeviceId={DeviceId}", userId, dto.ClientId, dto.DeviceId);

            await _authService.LogoutAsync(dto);

            _logger.LogInformation("Logout completed. UserId={UserID}, Client={Client}, DeviceId={DeviceId}", userId, dto.ClientId, dto.DeviceId);

            return Ok(ApiResponse<string>.Success("Logged out from this device"));
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> LogoutAll()
        {
            var userId = GetUserId();

            _logger.LogInformation("LogoutAll request received. UserId={UserID}", userId);

            await _authService.LogoutAllAsync(userId);

            _logger.LogInformation("LogoutALl completed. UserId={UserID}", userId);

            return Ok(ApiResponse<string>.Success("Logged out from all device"));
        }

        [HttpGet("sessions")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<List<SessionResponseDTO>>>> MySessions()
        {
            var userId = GetUserId();

            _logger.LogInformation("MySessions reqiest recevied. UserId={UserId}", userId);

            var sessions = await _authService.GetMySessionAsync(userId);

            _logger.LogInformation("Mysession completed. UserID={UserID}, Count={Count}", userId, sessions.Count);

            return Ok(ApiResponse<List<SessionResponseDTO>>.Success(sessions));
        }

        [HttpPost("sessions/revoke")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<string>>> RevokeSession([FromBody] RevokeSessionRequestDTO dto)
        {
            var userID = GetUserId();

            _logger.LogInformation("RevokedSession request received.UserId={UserId}, SessionId={SessionId}, Reason={Reason}", userID, dto.SessionId, dto.Reason);

            await _authService.RevokeSessionAsync(userID, dto.SessionId, dto.Reason);

            _logger.LogInformation("RevokedSession completed.UserId={UserId}, SessionId={SessionId}, Reason={Reason}", userID, dto.SessionId, dto.Reason);

            return Ok(ApiResponse<string>.Success("Session revoked."));
        }

        private Guid GetUserId()
        {
            ClaimsPrincipal user = HttpContext.User;
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(sub))
                throw new AppException("UserId claim missing.", 401);

            if (!Guid.TryParse(sub, out var userId))
                throw new AppException("Invalid UserId claim.", 401);
             
            return userId;
        }

    }

}
