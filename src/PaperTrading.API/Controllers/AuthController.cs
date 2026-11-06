using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PaperTrading.API.Auth;
using PaperTrading.API.Http;
using PaperTrading.Application.Auth;

namespace PaperTrading.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IMediator mediator, IHostEnvironment environment) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new RegisterCommand(request.Email, request.Password, request.DisplayName, ClientIp()),
            cancellationToken);

        return ApiResults.From(result, HttpContext, session =>
        {
            AppendRefreshCookie(session);
            return StatusCode(StatusCodes.Status201Created, ToResponse(session));
        });
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new LoginCommand(request.Email, request.Password, ClientIp()),
            cancellationToken);

        return ApiResults.From(result, HttpContext, session =>
        {
            AppendRefreshCookie(session);
            return Ok(ToResponse(session));
        });
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(RefreshCookie.Name, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return ApiResults.Problem(
                PaperTrading.Application.Common.Models.Error.Unauthorized(
                    "Auth.InvalidRefreshToken",
                    "Invalid or expired refresh token."),
                HttpContext);
        }

        var result = await mediator.Send(
            new RefreshSessionCommand(refreshToken, ClientIp()),
            cancellationToken);

        return ApiResults.From(result, HttpContext, session =>
        {
            AppendRefreshCookie(session);
            return Ok(ToResponse(session));
        });
    }

    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        Request.Cookies.TryGetValue(RefreshCookie.Name, out var refreshToken);
        await mediator.Send(new LogoutCommand(refreshToken), cancellationToken);
        RefreshCookie.Clear(Response, SecureCookie);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return ApiResults.Problem(
                PaperTrading.Application.Common.Models.Error.Unauthorized(
                    "Auth.InvalidToken",
                    "Authentication is required."),
                HttpContext);
        }

        var result = await mediator.Send(new GetCurrentUserQuery(userId), cancellationToken);
        return ApiResults.From(result, HttpContext, Ok);
    }

    private void AppendRefreshCookie(AuthSessionDto session)
        => RefreshCookie.Append(Response, session.RefreshToken, session.RefreshExpiresAtUtc, SecureCookie);

    private static AuthResponse ToResponse(AuthSessionDto session)
        => new(session.AccessToken, session.ExpiresAtUtc, session.User);

    private bool SecureCookie => !environment.IsEnvironment("Testing");

    private string? ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}

public sealed record RegisterRequest(string Email, string Password, string DisplayName);

public sealed record LoginRequest(string Email, string Password);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAtUtc, UserDto User);
