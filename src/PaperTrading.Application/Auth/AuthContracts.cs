namespace PaperTrading.Application.Auth;

public sealed record UserDto(Guid Id, string Email, string DisplayName);

public sealed record AuthSessionDto(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserDto User,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAtUtc);
