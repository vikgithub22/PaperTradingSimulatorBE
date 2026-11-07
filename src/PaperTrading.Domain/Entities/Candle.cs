using PaperTrading.Domain.Exceptions;

namespace PaperTrading.Domain.Entities;

public class Candle
{
    public Guid Id { get; private set; }
    public int InstrumentId { get; private set; }
    public DateTime OpenTimeUtc { get; private set; }
    public decimal Open { get; private set; }
    public decimal High { get; private set; }
    public decimal Low { get; private set; }
    public decimal Close { get; private set; }
    public long Volume { get; private set; }

    // EF Core parameterless constructor
    private Candle()
    {
    }

    public Candle(
        int instrumentId,
        DateTime openTimeUtc,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        long volume,
        Guid? id = null)
    {
        if (instrumentId <= 0)
        {
            throw new InvalidCandleException("Instrument ID must be positive.");
        }

        if (volume < 0)
        {
            throw new InvalidCandleException("Candle volume cannot be negative.");
        }

        ValidateOhlcInvariants(open, high, low, close);

        Id = id ?? Guid.CreateVersion7();
        InstrumentId = instrumentId;
        OpenTimeUtc = DateTime.SpecifyKind(openTimeUtc, DateTimeKind.Utc);
        Open = open;
        High = high;
        Low = low;
        Close = close;
        Volume = volume;
    }

    public static void ValidateOhlcInvariants(decimal open, decimal high, decimal low, decimal close)
    {
        if (low <= 0)
        {
            throw new InvalidCandleException($"Candle Low ({low}) must be greater than zero.");
        }

        if (high < low)
        {
            throw new InvalidCandleException($"Candle High ({high}) cannot be less than Low ({low}).");
        }

        if (high < open)
        {
            throw new InvalidCandleException($"Candle High ({high}) cannot be less than Open ({open}).");
        }

        if (high < close)
        {
            throw new InvalidCandleException($"Candle High ({high}) cannot be less than Close ({close}).");
        }

        if (low > open)
        {
            throw new InvalidCandleException($"Candle Low ({low}) cannot be greater than Open ({open}).");
        }

        if (low > close)
        {
            throw new InvalidCandleException($"Candle Low ({low}) cannot be greater than Close ({close}).");
        }
    }
}
