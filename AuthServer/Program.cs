using AuthServer.Data;
using AuthServer.Middlewares;
using AuthServer.Security;
using AuthServer.Services.Implemenations;
using AuthServer.Services.Interfaces;
using JwtAuth.Shared.Extensions;
using Microsoft.EntityFrameworkCore;

namespace AuthServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient();

            builder.Services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("AuthDb"));
            });

            builder.Services.AddScoped<IPasswordHasher,BcryptPasswordHasher>();
            builder.Services.AddScoped<IClientSecretHasher,ClientSecretHasher>();
            builder.Services.AddScoped<IJwtTokenService,JwtTokenService>();
            builder.Services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddJwtAuth(builder.Configuration);


            var app = builder.Build();
            app.UseMiddleware<ExceptionHandlingMiddleware>(); 
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization(); 

            app.MapControllers();

            app.Run();
        }
    }
}
