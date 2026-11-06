using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaperTrading.API.Http;
using PaperTrading.Application.Wallets;

namespace PaperTrading.API.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public sealed class WalletController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(WalletDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        var result = await mediator.Send(new GetWalletQuery(userId), cancellationToken);
        return ApiResults.From(result, HttpContext, Ok);
    }

    [HttpGet("ledger")]
    [ProducesResponseType(typeof(LedgerPageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Ledger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId))
        {
            return UnauthorizedProblem();
        }

        var result = await mediator.Send(new GetLedgerQuery(userId, page, pageSize), cancellationToken);
        return ApiResults.From(result, HttpContext, Ok);
    }

    private IActionResult UnauthorizedProblem()
        => ApiResults.Problem(
            PaperTrading.Application.Common.Models.Error.Unauthorized(
                "Auth.InvalidToken",
                "Authentication is required."),
            HttpContext);

    private bool TryGetUserId(out Guid userId)
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out userId);
    }
}
