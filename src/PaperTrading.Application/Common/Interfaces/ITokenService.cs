using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Common.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTimeOffset ExpiresAtUtc) GenerateAccessToken(
        Guid userId,
        string email,
        string displayName);

    Task<Result<(string RawRefreshToken, DateTimeOffset ExpiresAtUtc)>> CreateRefreshTokenAsync(
        Guid userId,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result<(string AccessToken, DateTimeOffset AccessExpiresAtUtc, string NewRefreshToken, DateTimeOffset RefreshExpiresAtUtc, Guid UserId, string Email, string DisplayName)>> RotateRefreshTokenAsync(
        string rawRefreshToken,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeRefreshTokenFamilyAsync(
        string rawRefreshToken,
        string reason,
        CancellationToken cancellationToken = default);
}
