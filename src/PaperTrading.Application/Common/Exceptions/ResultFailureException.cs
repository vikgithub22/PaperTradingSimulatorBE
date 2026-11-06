using PaperTrading.Application.Common.Models;

namespace PaperTrading.Application.Common.Exceptions;

/// <summary>
/// Thrown inside a unit-of-work transaction so the transaction rolls back,
/// then caught by the handler and turned back into a <see cref="Result"/>.
/// </summary>
public sealed class ResultFailureException : Exception
{
    public ResultFailureException(Error error)
        : base(error.Description)
    {
        Error = error;
    }

    public Error Error { get; }
}
