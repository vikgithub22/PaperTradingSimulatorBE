using PaperTrading.Domain.Enums;
using PaperTrading.Domain.Exceptions;

namespace PaperTrading.Domain.Entities;

public sealed class LedgerEntry
{
    public Guid Id { get; private set; }
    public Guid WalletId { get; private set; }
    public LedgerEntryDirection Direction { get; private set; }
    public LedgerEntryReason Reason { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? Reference { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    // Required by EF Core
    private LedgerEntry()
    {
    }

    internal static LedgerEntry Create(
        Guid walletId,
        LedgerEntryDirection direction,
        LedgerEntryReason reason,
        decimal amount,
        decimal balanceAfter,
        string? reference,
        string description,
        TimeProvider? timeProvider = null)
    {
        ValidateAmount(amount);

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be empty.", nameof(description));
        }

        if (description.Length > 200)
        {
            throw new ArgumentException("Description cannot exceed 200 characters.", nameof(description));
        }

        if (reference is not null && reference.Length > 64)
        {
            throw new ArgumentException("Reference cannot exceed 64 characters.", nameof(reference));
        }

        var now = (timeProvider ?? TimeProvider.System).GetUtcNow();

        return new LedgerEntry
        {
            Id = Guid.CreateVersion7(),
            WalletId = walletId,
            Direction = direction,
            Reason = reason,
            Amount = amount,
            BalanceAfter = balanceAfter,
            Reference = reference,
            Description = description,
            CreatedAtUtc = now
        };
    }

    public static void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
        {
            throw new InvalidMoneyAmountException(amount, "Amount must be strictly positive.");
        }

        // Check for at most 2 decimal places: reject if decimal * 100 != integer
        if (decimal.Floor(amount * 100m) != (amount * 100m))
        {
            throw new InvalidMoneyAmountException(amount, "Amount cannot have more than 2 decimal places.");
        }
    }
}
