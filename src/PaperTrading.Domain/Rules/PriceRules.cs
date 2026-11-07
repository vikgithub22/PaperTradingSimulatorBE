using PaperTrading.Domain.Exceptions;

namespace PaperTrading.Domain.Rules;

public static class PriceRules
{
    public const decimal DefaultTickSize = 0.05m;
    public const decimal DefaultDailyBandPercent = 10m;

    /// <summary>
    /// Rounds a price to the nearest tick grid using MidpointRounding.AwayFromZero.
    /// </summary>
    public static decimal RoundToTick(decimal price, decimal tickSize = DefaultTickSize)
    {
        if (tickSize <= 0)
        {
            throw new InvalidPriceException("Tick size must be greater than zero.");
        }

        var ticks = Math.Round(price / tickSize, MidpointRounding.AwayFromZero);
        return ticks * tickSize;
    }

    /// <summary>
    /// Checks whether the price lies exactly on the tick grid.
    /// </summary>
    public static bool IsOnTickGrid(decimal price, decimal tickSize = DefaultTickSize)
    {
        if (price <= 0 || tickSize <= 0)
        {
            return false;
        }

        var rounded = RoundToTick(price, tickSize);
        return Math.Abs(price - rounded) < 0.000001m;
    }

    /// <summary>
    /// Ensures that a price never drops below one minimum tick.
    /// </summary>
    public static decimal EnsureMinTick(decimal price, decimal tickSize = DefaultTickSize)
    {
        if (tickSize <= 0)
        {
            throw new InvalidPriceException("Tick size must be greater than zero.");
        }

        return Math.Max(tickSize, price);
    }

    /// <summary>
    /// Clamps the price within the daily price band (e.g., +/- 10% of PreviousClose),
    /// rounded to the tick grid, and never below one minimum tick.
    /// </summary>
    public static decimal ApplyDailyBand(
        decimal price,
        decimal previousClose,
        decimal bandPercent = DefaultDailyBandPercent,
        decimal tickSize = DefaultTickSize)
    {
        if (previousClose <= 0)
        {
            throw new InvalidPriceException("Previous close must be greater than zero.");
        }

        if (bandPercent < 0)
        {
            throw new InvalidPriceException("Band percent cannot be negative.");
        }

        if (tickSize <= 0)
        {
            throw new InvalidPriceException("Tick size must be greater than zero.");
        }

        var lowerBand = Math.Max(tickSize, RoundToTick(previousClose * (1m - bandPercent / 100m), tickSize));
        var upperBand = Math.Max(lowerBand, RoundToTick(previousClose * (1m + bandPercent / 100m), tickSize));

        var roundedPrice = RoundToTick(price, tickSize);
        var clampedPrice = Math.Clamp(roundedPrice, lowerBand, upperBand);

        return EnsureMinTick(clampedPrice, tickSize);
    }
}
