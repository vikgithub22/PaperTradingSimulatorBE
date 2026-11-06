using MediatR;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Auth;

public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserDto>>;

public sealed class GetCurrentUserQueryHandler(IIdentityService identity)
    : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await identity.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user.IsFailure)
        {
            return Result.Failure<UserDto>(user.Error);
        }

        return Result.Success(new UserDto(
            user.Value.UserId,
            user.Value.Email,
            user.Value.DisplayName));
    }
}
