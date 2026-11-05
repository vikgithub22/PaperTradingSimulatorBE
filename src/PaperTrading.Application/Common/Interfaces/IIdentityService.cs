using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<Result<Guid>> RegisterUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default);

    Task<Result<(Guid UserId, string Email, string DisplayName)>> ValidateUserCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    Task<Result<(Guid UserId, string Email, string DisplayName)>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
