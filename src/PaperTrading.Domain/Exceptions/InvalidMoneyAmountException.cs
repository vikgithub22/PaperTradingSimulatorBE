namespace PaperTrading.Domain.Exceptions;

public sealed class InvalidMoneyAmountException : DomainException
{
    public decimal Amount { get; }

    public InvalidMoneyAmountException(decimal amount, string reason)
        : base($"Invalid monetary amount {amount}: {reason}")
    {
        Amount = amount;
    }
}
