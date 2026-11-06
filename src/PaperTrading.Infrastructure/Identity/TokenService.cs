using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;
using PaperTrading.Infrastructure.Persistence;

namespace PaperTrading.Infrastructure.Identity;

public sealed class TokenService(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    JwtOptions jwtOptions,
    TimeProvider timeProvider) : ITokenService
{
    private const int RefreshTokenByteLength = 64;
    private readonly JsonWebTokenHandler _tokenHandler = new();

    public (string AccessToken, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(
        Guid userId,
        string email,
        string displayName)
    {
        // displayName is intentionally not a claim. Access tokens carry only sub, email and jti.
        _ = displayName;

        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            IssuedAt = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                SecurityAlgorithms.HmacSha256)
        };

        return (_tokenHandler.CreateToken(descriptor), expires);
    }

    public async Task<Result<(string RawRefreshToken, DateTimeOffset ExpiresAtUtc)>> CreateRefreshTokenAsync(
        Guid userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var (raw, entity) = CreateToken(userId, familyId: Guid.CreateVersion7(), ipAddress);
        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success((raw, entity.ExpiresAtUtc));
    }

    public async Task<Result<(string AccessToken, DateTimeOffset AccessExpiresAtUtc, string NewRefreshToken, DateTimeOffset RefreshExpiresAtUtc, Guid UserId, string Email, string DisplayName)>> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryHash(rawRefreshToken, out var hash))
        {
            return InvalidRefreshToken();
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var stored = await dbContext.RefreshTokens
                .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

            if (stored is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return InvalidRefreshToken();
            }

            var now = timeProvider.GetUtcNow();
            if (stored.RevokedAtUtc is not null)
            {
                await RevokeActiveFamilyAsync(stored.FamilyId, "Reuse detected", now, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return InvalidRefreshToken();
            }

            if (stored.ExpiresAtUtc <= now)
            {
                await transaction.RollbackAsync(cancellationToken);
                return InvalidRefreshToken();
            }

            var user = await userManager.FindByIdAsync(stored.UserId.ToString());
            if (user is null || string.IsNullOrWhiteSpace(user.Email))
            {
                await transaction.RollbackAsync(cancellationToken);
                return InvalidRefreshToken();
            }

            var (raw, replacement) = CreateToken(stored.UserId, stored.FamilyId, ipAddress);
            dbContext.RefreshTokens.Add(replacement);
            stored.RevokedAtUtc = now;
            stored.RevokedReason = "Rotated";
            stored.ReplacedByTokenId = replacement.Id;

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                await SafeRollbackAsync(transaction, cancellationToken);
                dbContext.ChangeTracker.Clear();
                return InvalidRefreshToken();
            }

            var (accessToken, accessExpiresAt) = GenerateAccessToken(user.Id, user.Email, user.DisplayName);
            return Result.Success((
                accessToken,
                accessExpiresAt,
                raw,
                replacement.ExpiresAtUtc,
                user.Id,
                user.Email,
                user.DisplayName));
        });
    }

    public async Task<Result> RevokeRefreshTokenFamilyAsync(
        string rawRefreshToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        if (!TryHash(rawRefreshToken, out var hash))
        {
            return Result.Success();
        }

        var stored = await dbContext.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            return Result.Success();
        }

        await RevokeActiveFamilyAsync(stored.FamilyId, reason, timeProvider.GetUtcNow(), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private (string Raw, RefreshToken Entity) CreateToken(Guid userId, Guid familyId, string? ipAddress)
    {
        var bytes = RandomNumberGenerator.GetBytes(RefreshTokenByteLength);
        var now = timeProvider.GetUtcNow();
        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            TokenHash = SHA256.HashData(bytes),
            FamilyId = familyId,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(jwtOptions.RefreshTokenDays),
            CreatedByIp = NormalizeIp(ipAddress)
        };

        return (Base64UrlEncode(bytes), entity);
    }

    private async Task RevokeActiveFamilyAsync(
        Guid familyId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var active = await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in active)
        {
            token.RevokedAtUtc = now;
            token.RevokedReason = reason.Length <= 100 ? reason : reason[..100];
        }
    }

    private static bool TryHash(string rawRefreshToken, out byte[] hash)
    {
        hash = [];
        if (string.IsNullOrWhiteSpace(rawRefreshToken) || !TryBase64UrlDecode(rawRefreshToken, out var bytes))
        {
            return false;
        }

        if (bytes.Length != RefreshTokenByteLength)
        {
            return false;
        }

        hash = SHA256.HashData(bytes);
        return true;
    }

    private static Result<(string AccessToken, DateTimeOffset AccessExpiresAtUtc, string NewRefreshToken, DateTimeOffset RefreshExpiresAtUtc, Guid UserId, string Email, string DisplayName)> InvalidRefreshToken()
        => Result.Failure<(string, DateTimeOffset, string, DateTimeOffset, Guid, string, string)>(
            Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token."));

    private static async Task SafeRollbackAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken);
        }
        catch (Exception)
        {
            // The transaction may already be aborted after a concurrency failure.
        }
    }

    private static string? NormalizeIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return null;
        }

        return ipAddress.Length <= 45 ? ipAddress : ipAddress[..45];
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryBase64UrlDecode(string value, out byte[] bytes)
    {
        bytes = [];
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = (padded.Length % 4) switch
            {
                2 => padded + "==",
                3 => padded + "=",
                0 => padded,
                _ => throw new FormatException("Invalid base64url length.")
            };

            bytes = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
