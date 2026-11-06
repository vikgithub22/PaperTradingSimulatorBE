using MediatR;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Auth;

public sealed record RefreshSessionCommand(
    string RefreshToken,
    string? IpAddress) : IRequest<Result<AuthSessionDto>>;

public sealed class RefreshSessionCommandHandler(ITokenService tokens)
    : IRequestHandler<RefreshSessionCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(
        RefreshSessionCommand request,
        CancellationToken cancellationToken)
    {
        var rotated = await tokens.RotateRefreshTokenAsync(
            request.RefreshToken,
            request.IpAddress,
            cancellationToken);

        if (rotated.IsFailure)
        {
            return Result.Failure<AuthSessionDto>(rotated.Error);
        }

        var value = rotated.Value;
        return Result.Success(new AuthSessionDto(
            value.AccessToken,
            value.AccessExpiresAtUtc,
            new UserDto(value.UserId, value.Email, value.DisplayName),
            value.NewRefreshToken,
            value.RefreshExpiresAtUtc));
    }
}
