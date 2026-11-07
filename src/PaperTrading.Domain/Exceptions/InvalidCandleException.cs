namespace PaperTrading.Domain.Exceptions;

public sealed class InvalidCandleException : DomainException
{
    public InvalidCandleException(string message) : base(message)
    {
    }
}
