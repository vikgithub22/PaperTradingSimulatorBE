using FluentValidation;
using MediatR;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Wallets;

public sealed record WalletDto(decimal Balance, string Currency);

public sealed record LedgerItemDto(
    DateTimeOffset CreatedAtUtc,
    string Direction,
    string Reason,
    string Description,
    decimal Amount,
    decimal BalanceAfter);

public sealed record LedgerPageDto(
    IReadOnlyList<LedgerItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record GetWalletQuery(Guid UserId) : IRequest<Result<WalletDto>>;

public sealed class GetWalletQueryHandler(IWalletRepository wallets)
    : IRequestHandler<GetWalletQuery, Result<WalletDto>>
{
    public async Task<Result<WalletDto>> Handle(
        GetWalletQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await wallets.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet is null)
        {
            return Result.Failure<WalletDto>(Error.NotFound("Wallet.NotFound", "Wallet was not found."));
        }

        return Result.Success(new WalletDto(wallet.Balance, "INR"));
    }
}

public sealed record GetLedgerQuery(Guid UserId, int Page, int PageSize) : IRequest<Result<LedgerPageDto>>;

public sealed class GetLedgerQueryValidator : AbstractValidator<GetLedgerQuery>
{
    public const int MaxPageSize = 100;

    public GetLedgerQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}

public sealed class GetLedgerQueryHandler(IWalletRepository wallets)
    : IRequestHandler<GetLedgerQuery, Result<LedgerPageDto>>
{
    public async Task<Result<LedgerPageDto>> Handle(
        GetLedgerQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await wallets.GetByUserIdAsync(request.UserId, cancellationToken);
        if (wallet is null)
        {
            return Result.Failure<LedgerPageDto>(Error.NotFound("Wallet.NotFound", "Wallet was not found."));
        }

        var (items, totalCount) = await wallets.GetLedgerEntriesPagedAsync(
            wallet.Id,
            request.Page,
            request.PageSize,
            cancellationToken);

        var dtos = items
            .Select(entry => new LedgerItemDto(
                entry.CreatedAtUtc,
                entry.Direction.ToString(),
                entry.Reason.ToString(),
                entry.Description,
                entry.Amount,
                entry.BalanceAfter))
            .ToList();

        return Result.Success(new LedgerPageDto(dtos, request.Page, request.PageSize, totalCount));
    }
}
