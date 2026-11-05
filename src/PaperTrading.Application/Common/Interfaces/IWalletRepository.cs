using PaperTrading.Domain.Entities;

namespace PaperTrading.Application.Common.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<LedgerEntry> Items, int TotalCount)> GetLedgerEntriesPagedAsync(
        Guid walletId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
