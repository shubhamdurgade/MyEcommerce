using JwtAuth.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace JwtAuth.Shared.Extensions
{
    public static class JwrAuthServiceCollectionExtension
    {
        private const string JwtSectionName = "JWT";
        private const string RefreshTokenSectionName = "RefreshToken";
        private const string SharedSectionName = "JwtAuthShared";

        public static IServiceCollection AddJwtAuth(this IServiceCollection services,IConfiguration config, Action<JwtAuthSharedOptions>? configure = null)
        {
            services.Configure<JwtSettings>(config.GetSection(JwtSectionName));
            services.Configure<RefreshTokenSettings>(config.GetSection(RefreshTokenSectionName));
            services.Configure<JwtAuthSharedOptions>(config.GetSection(SharedSectionName));

            var jwtSettings = config.GetSection(JwtSectionName).Get<JwtSettings>() ?? throw new InvalidOperationException("JWT configuration section is missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Issuer))
                throw new InvalidOperationException("JWT: Issuer missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.Audience))
                throw new InvalidOperationException("JWT: Audience missing.");

            if (string.IsNullOrWhiteSpace(jwtSettings.SigningKey))
                throw new InvalidOperationException("JWT: SigningKey missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey));

            var sharedOptions = config.GetSection(SharedSectionName).Get<JwtAuthSharedOptions>()  ?? new JwtAuthSharedOptions();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = sharedOptions.RequireHttpsMetadata;
                    
                    options.SaveToken = sharedOptions.SaveToken;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtSettings.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtSettings.Audience,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,

                        ValidateLifetime = true,

                        ClockSkew = TimeSpan.FromSeconds(sharedOptions.ClockSkewSeconds)
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnChallenge = async context =>
                        {
                            context.HandleResponse();

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";

                            var bodyObj = new
                            {
                                IsSuccess = false,
                                Status = StatusCodes.Status401Unauthorized,
                                Message = "Unauthorized. Please provide a valid access token."
                            };

                            var json = JsonSerializer.Serialize(bodyObj, sharedOptions.JsonSerializerOptions);

                            await context.Response.WriteAsync(json);
                        },

                        OnForbidden = async context =>
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            var bodyObj = new
                            {
                                IsSuccess = false,
                                Status = StatusCodes.Status403Forbidden,
                                Message = "Forbidden. You do not have permission to access."
                            };

                            var json = JsonSerializer.Serialize(bodyObj, sharedOptions.JsonSerializerOptions);

                            await context.Response.WriteAsync(json);
                        },

                        OnAuthenticationFailed = context =>
                        {
                            if (sharedOptions.EnableAuthFailureLogging)
                                return Task.CompletedTask;

                            var loggerFactory = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>();
                            var logger = loggerFactory.CreateLogger(sharedOptions.LoggerCategoryName);

                            var http = context.HttpContext;

                            logger.LogWarning(context.Exception,
                                "JWT authentication failed. Path = {Path}, Method = {Method}, RemoteIP = {RemoteIp}, Scheme={Scheme},Auth={Auth}",
                                http.Request.Path.Value,
                                http.Request.Method,
                                http.Connection.RemoteIpAddress?.ToString(),
                                http.Request.Scheme,
                                http.Request.Headers.ContainsKey("Authorization"));
                            return Task.CompletedTask;
                        }
                    }; 
                });

            services.AddAuthorization(options =>
            { 
                if (sharedOptions.AddsharedPolicies)
                    return;

                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                options.AddPolicy("CustomOnly", p => p.RequireRole("Custom"));
                options.AddPolicy("HasClientId", p => p.RequireRole("client_id"));
                options.AddPolicy("HasSessionId", p => p.RequireRole("sid"));
                options.AddPolicy("AdminOrSeller", p => p.RequireRole("Admin", "Seller"));
                options.AddPolicy("AdminANdSeller", p =>
                {
                    p.RequireRole("Admin");
                    p.RequireRole("Seller");
                });

                options.AddPolicy("CustomerWithClient", p =>
                {
                    p.RequireRole("Customer");
                    p.RequireRole("Client_id");
                });
            });

            return services;
        }
    }
}
