using AuthServer.Common.Exceptions;
using AuthServer.Data;
using AuthServer.DTOs.Requests;
using AuthServer.DTOs.Responses;
using AuthServer.Entities;
using AuthServer.Security;
using AuthServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System.Text.Json;
using System.Xml;

namespace AuthServer.Services.Implemenations
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _dbContext;

        private readonly ILogger<AuthService> _logger;

        private readonly IPasswordHasher _passwordHasher;

        private readonly IJwtTokenService _jwtTokenService;

        private readonly IRefreshTokenService _refreshTokenService;

        private readonly IClientSecretHasher _clientSecretHasher;

        private readonly HttpClient _httpClient;

        public AuthService(AuthDbContext db, ILogger<AuthService> logger, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, 
            IRefreshTokenService refreshTokenService, IClientSecretHasher clientSecretHasher, IHttpClientFactory httpClientFactory)
        {
            _dbContext = db;
            _logger = logger;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _refreshTokenService = refreshTokenService;
            _clientSecretHasher = clientSecretHasher;
            _httpClient = httpClientFactory.CreateClient();
        }
        
        public async Task RegisterAsync(RegisterRequestDTO dto)
        {
            try
            { 
                var emailExists = await _dbContext.Users.AnyAsync(x => x.Email == dto.Email);
                if (emailExists)  throw new AppException("Email already exists.");

                var defaultsRole = await _dbContext.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
                if (defaultsRole == null) throw new AppException("Default role is not Configured.", 500);
            
                var user = new AppUser
                {
                    Id = Guid.NewGuid(),
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = _passwordHasher.Hash(dto.Password),
                    CreatedUtc = DateTime.UtcNow,
                };

                _dbContext.Users.Add(user);
                user.IsActive = true;

                _dbContext.UserRoles.Add(
                    new AppUserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = defaultsRole.Id,
                        AssignedByUserId = null,
                        AssignedUtc = DateTime.UtcNow,
                        Notes = "Self registration"
                    }
                );

                await _dbContext.SaveChangesAsync(); ;
                _logger.LogInformation("User registered succesfully. UserId = {UserID}, Email = {EMail}", user.Id, user.Email);
               
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex,"Error occured while registering user. Email = {EMail}",dto.Email);
                throw new AppException("Unable to register use.please try again.", 500);
            }
        }

        public async Task<TokenResponseDTO> LoginAsync(LoginRequestDTO dto, HttpContext httpContext)
        {

            try
            { 
                var client = await ValidateClientCredentialAsync(dto.ClientId, dto.ClientSecret);
                var user = await _dbContext.Users
                            .AsNoTracking()
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null || !user.IsActive)
                    throw new NotImplementedException();

                if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
                    throw new AppException("Invalid credentials");

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                (string? IpAddress, string location) = await GetLocationAsync(ipAddress);
                var session = new UserSession
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ClientAppId = client.Id,
                    IpAddress = ipAddress,
                    LoginLocation = location,
                    UserAgent = userAgent,
                    CreatedUtc = DateTime.UtcNow,
                    LastSeenUtc = DateTime.UtcNow
                };
                _dbContext.UserSessions.Add(session);

                //create refresh token: 
                // - raw refresh token is retunred to client

                var rawRefreshToken = _refreshTokenService.GenerateRawToken();
                var refreshTokenHash = _refreshTokenService.HashToken(rawRefreshToken);
                var expiredAtUtc = _refreshTokenService.GetExpiryUtc();

                var refreshEntity = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    SessionId = session.Id,
                    TokenHash = refreshTokenHash,
                    CreatedUtc = DateTime.UtcNow,
                    ExpiresUtc = expiredAtUtc
                };

                _dbContext.RefreshTokens.Add(refreshEntity);

                // create JWT access token (short-lived) with role claims

                var roles = user.UserRoles.Select(x => x.Role.Name).ToList();
                var (jwt, jwtExp) = _jwtTokenService.CreateAccessToken(user, roles, dto.ClientId, session.Id);

                // persist session + reshresh toke 

                await _dbContext.SaveChangesAsync(); ;

                _logger.LogInformation("Login success. Client={ClientId}, UserId={UserId}, SessionId={SessionId}, DeviceId={DevicedId}, IpAddress={IpAddress}",
                    dto.ClientId, user.Id, session.Id, dto.DeviceId, ipAddress);

                return new TokenResponseDTO
                {
                    SessionId = session.Id,
                    AccesToken = jwt,
                    AccessTokenExperisUtc = jwtExp ,
                    RefreshToken = rawRefreshToken,
                    RefreshTokenExperisUtcUtc = refreshEntity.ExpiresUtc
                };

            }
            catch (AppException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed, Email={Email}, ClientId={ClientId}", dto.Email, dto.ClientId);
                throw new AppException("Unable to login.Please try again.", 500);
            }
        }

        public Task<List<SessionResponseDTO>> GetMySessionAsync(Guid userId)
        {
            throw new NotImplementedException();
        }
        
        public async Task LogoutAsync(LogoutRequestDTO dto)
        {
            try
            {
                var client = await ValidateClientCredentialAsync(dto.ClientId, dto.ClientSecret);

                var tokenHash = _refreshTokenService.HashToken(dto.RefreshToken);

                var existing = await _dbContext.RefreshTokens
                                    .Include(rt => rt.Session)
                                    .ThenInclude(s => s.ClientApp)
                                    .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (existing == null) return;

                if (existing.Session == null)
                    throw new AppException("Invalid logout session.", 401);

                if (existing.Session.ClientAppId != client.Id)
                    throw new AppException("Logout token does not belong to this client.", 401);

                if (!string.Equals(existing.Session.DeviceId, dto.DeviceId, StringComparison.OrdinalIgnoreCase))
                    throw new AppException("Logout token does not match this device.", 401);

                if (existing.Session.RevokedUtc != null)
                    return;

                if (existing.Session.ClientApp == null || !existing.Session.ClientApp.IsActive)
                    throw new AppException("Invalid client.", 401);

                existing.Session.RevokedUtc = DateTime.UtcNow;

                var tokens = await _dbContext.RefreshTokens
                                .Where(rt => rt.SessionId == existing.SessionId && rt.RevokedUtc == null)
                                .ToListAsync();

                foreach (var token in tokens)
                {
                    token.RevokedUtc = DateTime.UtcNow;
                    token.RevokedReason = "Logout";
                }

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Logout succesfull. ClientId = {ClientId}, SessionId={SessionId}, DeviceId={DeviceId}"
                    , dto.ClientId, existing.SessionId, dto.DeviceId);

            }
            catch (AppException) { throw; }
            catch (Exception ex) { _logger.LogInformation(ex, "Logout failed. ClientId={ClientId}, DeviceId={DeviceId}", dto.ClientId,dto.DeviceId); throw new AppException("Unable to logout . Please try again.", 500); }
        }

        public async Task<TokenResponseDTO> RefreshAsync(RefreshRequestDTO dto, HttpContext httpContext)
        {
            try
            {
                var client = await ValidateClientCredentialAsync(dto.ClientId, dto.ClientSecret);

                var tokenHash = _refreshTokenService.HashToken(dto.RefreshToken);
                await using var tx = await _dbContext.Database.BeginTransactionAsync();

                var existing = await _dbContext.RefreshTokens
                                    .Include(rt => rt.Session)
                                    .ThenInclude(s => s.User)
                                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);

                if (existing == null)
                    throw new AppException("Invalid refresh token.", 401);

                if (existing.Session == null)
                    throw new AppException("Invalid refresh token session.", 401);

                if(existing.Session.ClientAppId != client.Id)
                    throw new AppException("Refresh token does not match this client application.", 401);

                if (existing.Session.RevokedUtc != null)
                    throw new AppException("Refresh token does not match this device.", 401);   

                if(!string.Equals(existing.Session.DeviceId, dto.DeviceId, StringComparison.Ordinal))
                    throw new AppException("Refresh token does not match this device.", 401);

                if(existing.Session.User == null || !existing.Session.User.IsActive)
                    throw new AppException("User account is not active.", 401);

                if (existing.IsRevoked)
                {
                    existing.Session.RevokedUtc = DateTime.UtcNow;

                    var activeSessionToken = await _dbContext.RefreshTokens
                        .Where(x => x.SessionId == existing.SessionId && !x.IsRevoked)
                        .ToListAsync();

                    foreach (var token in activeSessionToken)
                    {
                        token.RevokedUtc = DateTime.UtcNow;
                        token.RevokedReason = "ReuseDetected";
                    }
                     
                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync(); 

                    _logger.LogWarning("Refresh token reuse detected. All tokens for the session revoked. SessionId={SessionId}, UserId={UserId}, ClientId={ClientId}, DeviceId={DeviceId}",
                        existing.SessionId, existing.Session.UserId, client.Id, dto.DeviceId);

                    throw new AppException("Refresh token reuse detected.Please login again.", 401);
                }

                if(existing.IsExpired)
                    throw new AppException("Refresh token has expired. Please login again.", 401);

                var newRawRefreshToken = _refreshTokenService.GenerateRawToken();
                var newRefreshTokenHash = _refreshTokenService.HashToken(newRawRefreshToken);
                var expiredAtUtc = _refreshTokenService.GetExpiryUtc();

                var newEntity  = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    SessionId = existing.SessionId,
                    TokenHash = newRefreshTokenHash,
                    ParentTokenId = existing.Id,
                    CreatedUtc = DateTime.UtcNow,
                    ExpiresUtc = expiredAtUtc
                };

                existing.RevokedUtc = DateTime.UtcNow;
                existing.RevokedReason = "Rotated";
                existing.ReplaceByTokenId = newEntity.Id;

                _dbContext.RefreshTokens.Add(newEntity);

                existing.Session.LastSeenUtc = DateTime.UtcNow;
                
                existing.Session.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                
                existing.Session.UserAgent = httpContext.Request.Headers.UserAgent.ToString();
                
                (string? IpAddress, string location) = await GetLocationAsync(existing.Session.IpAddress);
                
                existing.Session.IpAddress = IpAddress;
                existing.Session.LoginLocation = location;

                var roles = await _dbContext.UserRoles
                    .Where(ur => ur.UserId == existing.Session.UserId)
                    .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Name)
                    .ToListAsync();

                var (jwt, jwtExp) = _jwtTokenService.CreateAccessToken(existing.Session.User!, roles, dto.ClientId, existing.SessionId);

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();

                _logger.LogInformation("Refresh token rotated. SessionId={SessionId}, UserId={UserId}, ClientId={ClientId}, DeviceId={DeviceId}",
                    existing.SessionId, existing.Session.UserId, client.Id, dto.DeviceId);

                return new TokenResponseDTO
                {
                    AccessTokenExperisUtc = jwtExp,
                    AccesToken = jwt,
                    RefreshToken = newRawRefreshToken,
                    RefreshTokenExperisUtcUtc = newEntity.ExpiresUtc,
                    SessionId = existing.SessionId
                };
            }
            catch(AppException ex)
            {
                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Refresh failed. ClientId={ClientId}, DeviceId={DeviceId}", dto.ClientId, dto.DeviceId);
                throw new AppException("Unable to refresh token. Please try again.", 500);
            }
        }

        public Task RevokeSessionAsync(Guid userId, Guid sessionId, string reason)
        {
            throw new NotImplementedException();
        }

        private async Task<ClientApp> ValidateClientCredentialAsync(string clientId, string clientSecret)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new AppException("ClientId is required.");

            if (string.IsNullOrWhiteSpace(clientSecret))
                throw new AppException("ClientSecret is required.");

            var client = await _dbContext.ClientApps.FirstOrDefaultAsync(c => c.ClientId == clientId && c.IsActive);
            if (client == null)
                throw new AppException("Invalid client.");

            if(!_clientSecretHasher.Verify(clientSecret,client.ClientSecretHash))
                throw new AppException($"Invalid client credentials.",401);

            return client;
        }

        private async Task<(string?, string)> GetLocationAsync(string? ipAddress)
        {
            try
            {
                if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress == "127.0.0.1")
                {
                    ipAddress = await _httpClient.GetStringAsync("http://api.ipify.org");
                }

                var response = await _httpClient.GetStringAsync($"http://ip-api.com/json/{ipAddress}");
                var locationData = JsonSerializer.Deserialize<Dictionary<string, string>>(response);

                if (locationData!=null &&
                    locationData.TryGetValue("city", out var city) &&
                    locationData.TryGetValue("regionName", out var state) &&
                    locationData.TryGetValue("country", out var country) &&
                    locationData.TryGetValue("zip", out var zip))
                {
                    return (ipAddress, $"{country}-{state}-{city}-{zip}");
                }

                return (ipAddress, "Unknown Location");
            }
            catch (Exception ex)
            {
                return (ipAddress, "Unknown Location");
            }
        }
    }
}
