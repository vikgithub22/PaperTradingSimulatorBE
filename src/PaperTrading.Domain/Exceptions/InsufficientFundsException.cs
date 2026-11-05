namespace PaperTrading.Domain.Exceptions;

public sealed class InsufficientFundsException : DomainException
{
    public Guid WalletId { get; }
    public decimal Balance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(Guid walletId, decimal balance, decimal requestedAmount)
        : base($"Insufficient funds in wallet '{walletId}'. Available balance: {balance:F2}, requested debit: {requestedAmount:F2}.")
    {
        WalletId = walletId;
        Balance = balance;
        RequestedAmount = requestedAmount;
    }
}
