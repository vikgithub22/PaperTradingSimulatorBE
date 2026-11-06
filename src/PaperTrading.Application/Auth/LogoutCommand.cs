using MediatR;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Auth;

public sealed record LogoutCommand(string? RefreshToken) : IRequest<Result>;

public sealed class LogoutCommandHandler(ITokenService tokens)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            await tokens.RevokeRefreshTokenFamilyAsync(
                request.RefreshToken,
                "Logout",
                cancellationToken);
        }

        return Result.Success();
    }
}
