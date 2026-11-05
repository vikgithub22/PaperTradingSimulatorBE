using PaperTrading.Domain.Enums;
using PaperTrading.Domain.Exceptions;

namespace PaperTrading.Domain.Entities;

public sealed class Wallet
{
    private readonly List<LedgerEntry> _ledgerEntries = [];

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public decimal Balance { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<LedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    // Required by EF Core
    private Wallet()
    {
    }

    public static Wallet Create(
        Guid userId,
        decimal initialBalance,
        TimeProvider? timeProvider = null,
        string description = "Starting balance")
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();

        var wallet = new Wallet
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Balance = 0m,
            CreatedAtUtc = now
        };

        if (initialBalance > 0m)
        {
            wallet.Credit(initialBalance, LedgerEntryReason.InitialDeposit, description, null, timeProvider);
        }

        return wallet;
    }

    public LedgerEntry Credit(
        decimal amount,
        LedgerEntryReason reason,
        string description,
        string? reference = null,
        TimeProvider? timeProvider = null)
    {
        LedgerEntry.ValidateAmount(amount);

        Balance += amount;

        var entry = LedgerEntry.Create(
            walletId: Id,
            direction: LedgerEntryDirection.Credit,
            reason: reason,
            amount: amount,
            balanceAfter: Balance,
            reference: reference,
            description: description,
            timeProvider: timeProvider);

        _ledgerEntries.Add(entry);
        return entry;
    }

    public LedgerEntry Debit(
        decimal amount,
        LedgerEntryReason reason,
        string description,
        string? reference = null,
        TimeProvider? timeProvider = null)
    {
        LedgerEntry.ValidateAmount(amount);

        if (Balance < amount)
        {
            throw new InsufficientFundsException(Id, Balance, amount);
        }

        Balance -= amount;

        var entry = LedgerEntry.Create(
            walletId: Id,
            direction: LedgerEntryDirection.Debit,
            reason: reason,
            amount: amount,
            balanceAfter: Balance,
            reference: reference,
            description: description,
            timeProvider: timeProvider);

        _ledgerEntries.Add(entry);
        return entry;
    }
}
