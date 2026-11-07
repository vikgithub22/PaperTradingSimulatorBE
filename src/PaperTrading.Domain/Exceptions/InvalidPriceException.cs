namespace PaperTrading.Domain.Exceptions;

public sealed class InvalidPriceException : DomainException
{
    public InvalidPriceException(string message) : base(message)
    {
    }
}
