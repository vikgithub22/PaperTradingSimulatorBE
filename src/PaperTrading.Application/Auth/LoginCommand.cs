using FluentValidation;
using MediatR;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Auth;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress) : IRequest<Result<AuthSessionDto>>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}

public sealed class LoginCommandHandler(
    IIdentityService identity,
    ITokenService tokens) : IRequestHandler<LoginCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await identity.ValidateUserCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user.IsFailure)
        {
            return Result.Failure<AuthSessionDto>(user.Error);
        }

        var (accessToken, accessExpiresAt) = tokens.GenerateAccessToken(
            user.Value.UserId,
            user.Value.Email,
            user.Value.DisplayName);

        var refresh = await tokens.CreateRefreshTokenAsync(
            user.Value.UserId,
            request.IpAddress,
            cancellationToken);

        if (refresh.IsFailure)
        {
            return Result.Failure<AuthSessionDto>(refresh.Error);
        }

        return Result.Success(new AuthSessionDto(
            accessToken,
            accessExpiresAt,
            new UserDto(user.Value.UserId, user.Value.Email, user.Value.DisplayName),
            refresh.Value.RawRefreshToken,
            refresh.Value.ExpiresAtUtc));
    }
}
