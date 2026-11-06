using FluentValidation;
using MediatR;
using PaperTrading.Application.Common.Exceptions;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;
using PaperTrading.Application.Configuration;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Application.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName,
    string? IpAddress) : IRequest<Result<AuthSessionDto>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a digit.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(30);
    }
}

public sealed class RegisterCommandHandler(
    IIdentityService identity,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    ITokenService tokens,
    WalletOptions walletOptions,
    TimeProvider timeProvider) : IRequestHandler<RegisterCommand, Result<AuthSessionDto>>
{
    public async Task<Result<AuthSessionDto>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var created = await identity.RegisterUserAsync(
                    request.Email,
                    request.Password,
                    request.DisplayName,
                    ct);

                if (created.IsFailure)
                {
                    throw new ResultFailureException(created.Error);
                }

                var wallet = Wallet.Create(
                    created.Value,
                    walletOptions.StartingBalance,
                    timeProvider,
                    "Starting balance");

                await wallets.AddAsync(wallet, ct);

                var profile = await identity.GetUserByIdAsync(created.Value, ct);
                if (profile.IsFailure)
                {
                    throw new ResultFailureException(profile.Error);
                }

                var (accessToken, accessExpiresAt) = tokens.GenerateAccessToken(
                    profile.Value.UserId,
                    profile.Value.Email,
                    profile.Value.DisplayName);

                var refresh = await tokens.CreateRefreshTokenAsync(
                    profile.Value.UserId,
                    request.IpAddress,
                    ct);

                if (refresh.IsFailure)
                {
                    throw new ResultFailureException(refresh.Error);
                }

                return Result.Success(new AuthSessionDto(
                    accessToken,
                    accessExpiresAt,
                    new UserDto(profile.Value.UserId, profile.Value.Email, profile.Value.DisplayName),
                    refresh.Value.RawRefreshToken,
                    refresh.Value.ExpiresAtUtc));
            }, cancellationToken);
        }
        catch (ResultFailureException exception)
        {
            return Result.Failure<AuthSessionDto>(exception.Error);
        }
    }
}
