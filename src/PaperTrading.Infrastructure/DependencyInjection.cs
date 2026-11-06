using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Configuration;
using PaperTrading.Infrastructure.Identity;
using PaperTrading.Infrastructure.Persistence;

namespace PaperTrading.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentityCore<ApplicationUser>(options =>
        {
            // Password policy: min 8 chars, 1 uppercase, 1 lowercase, 1 digit
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = false;

            // Lockout policy: 5 failed attempts, 5 minutes lockout
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User policy
            options.User.RequireUniqueEmail = true;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddEntityFrameworkStores<AppDbContext>();

        services.AddSingleton(CreateJwtOptions(configuration));
        services.AddSingleton(CreateWalletOptions(configuration));
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IWalletRepository, WalletRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    private static JwtOptions CreateJwtOptions(IConfiguration configuration)
    {
        var options = new JwtOptions
        {
            Issuer = configuration["Jwt:Issuer"] ?? "PaperTrading",
            Audience = configuration["Jwt:Audience"] ?? "PaperTrading",
            SigningKey = configuration["Jwt:SigningKey"] ?? string.Empty,
            AccessTokenMinutes = ParsePositiveInt(configuration["Jwt:AccessTokenMinutes"], 15),
            RefreshTokenDays = ParsePositiveInt(configuration["Jwt:RefreshTokenDays"], 7)
        };

        if (Encoding.UTF8.GetByteCount(options.SigningKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 UTF-8 bytes. " +
                "Set it with: dotnet user-secrets set \"Jwt:SigningKey\" \"<key-at-least-32-chars>\" --project src/PaperTrading.API");
        }

        return options;
    }

    private static WalletOptions CreateWalletOptions(IConfiguration configuration)
    {
        var options = new WalletOptions();
        var configured = configuration["Wallet:StartingBalance"];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (!decimal.TryParse(configured, NumberStyles.Number, CultureInfo.InvariantCulture, out var starting)
                || starting <= 0m
                || decimal.Floor(starting * 100m) != starting * 100m)
            {
                throw new InvalidOperationException(
                    "Wallet:StartingBalance must be a positive decimal with at most 2 decimal places.");
            }

            options.StartingBalance = starting;
        }

        return options;
    }

    private static int ParsePositiveInt(string? value, int fallback)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : fallback;
}
