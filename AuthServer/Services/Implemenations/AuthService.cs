using AuthServer.Common.Exceptions;
using AuthServer.Data;
using AuthServer.DTOs.Requests;
using AuthServer.DTOs.Responses;
using AuthServer.Entities;
using AuthServer.Security;
using AuthServer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthServer.Services.Implemenations
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _db;

        private readonly ILogger<AuthService> _logger;

        private readonly IPasswordHasher _passwordHasher;

        private readonly IJwtTokenService _jwtTokenService;

        private readonly IRefreshTokenService _refreshTokenService;

        private readonly IClientSecretHasher _clientSecretHasher;

        private readonly HttpClient _httpClient;

        public AuthService(AuthDbContext db, ILogger<AuthService> logger, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, 
            IRefreshTokenService refreshTokenService, IClientSecretHasher clientSecretHasher, IHttpClientFactory httpClientFactory)
        {
            _db = db;
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
                var emailExists = await _db.Users.AnyAsync(x => x.Email == dto.Email);
                if (emailExists)  throw new AppException("Email already exists.");

                var defaultsRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Customer");
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

                _db.Users.Add(user);
                user.IsActive = true;

                _db.UserRoles.Add(
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

                await _db.SaveChangesAsync(); ;
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

        public Task<List<SessionResponseDTO>> GetMySessionAsync(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<TokenResponseDTO> LoginAsync(LoginRequestDTO Dto, HttpContext httpContext)
        {
            throw new NotImplementedException();
        }

        public Task LogoutAsync(LogoutRequestDTO dto)
        {
            throw new NotImplementedException();
        }

        public Task<TokenResponseDTO> RefreshAsync(RefreshRequestDTO dto, HttpContext httpContext)
        {
            throw new NotImplementedException();
        }


        public Task RevokeSessionAsync(Guid userId, Guid sessionId, string reason)
        {
            throw new NotImplementedException();
        }
    }
}
