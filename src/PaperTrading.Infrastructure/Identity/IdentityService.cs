using Microsoft.AspNetCore.Identity;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _timeProvider;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        TimeProvider? timeProvider = null)
    {
        _userManager = userManager;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<Result<Guid>> RegisterUserAsync(
        string email,
        string password,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            return Result.Failure<Guid>(Error.Conflict("User.DuplicateEmail", "A user with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.CreateVersion7(),
            UserName = email,
            Email = email,
            DisplayName = displayName,
            CreatedAtUtc = _timeProvider.GetUtcNow()
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(e => e.Code, e => e.Description)
                .ToDictionary(g => g.Key, g => g.ToArray());

            return Result.Failure<Guid>(new ValidationError(errors));
        }

        return Result.Success(user.Id);
    }

    public async Task<Result<(Guid UserId, string Email, string DisplayName)>> ValidateUserCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            // Generic error: do not reveal email existence
            return Result.Failure<(Guid UserId, string Email, string DisplayName)>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return Result.Failure<(Guid UserId, string Email, string DisplayName)>(
                Error.Forbidden("Auth.LockedOut", "Account is locked due to multiple failed login attempts. Try again later."));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return Result.Failure<(Guid UserId, string Email, string DisplayName)>(
                    Error.Forbidden("Auth.LockedOut", "Account is locked due to multiple failed login attempts. Try again later."));
            }

            return Result.Failure<(Guid UserId, string Email, string DisplayName)>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        return Result.Success((user.Id, user.Email!, user.DisplayName));
    }

    public async Task<Result<(Guid UserId, string Email, string DisplayName)>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return Result.Failure<(Guid UserId, string Email, string DisplayName)>(
                Error.NotFound("User.NotFound", $"User '{userId}' was not found."));
        }

        return Result.Success((user.Id, user.Email!, user.DisplayName));
    }
}
