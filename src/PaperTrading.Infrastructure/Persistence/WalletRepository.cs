using Microsoft.EntityFrameworkCore;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence;

internal sealed class WalletRepository(AppDbContext dbContext) : IWalletRepository
{
    public async Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await dbContext.Wallets
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

    public async Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
        => await dbContext.Wallets.AddAsync(wallet, cancellationToken);

    public async Task<(IReadOnlyList<LedgerEntry> Items, int TotalCount)> GetLedgerEntriesPagedAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.WalletId == walletId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ThenByDescending(e => e.Id);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
