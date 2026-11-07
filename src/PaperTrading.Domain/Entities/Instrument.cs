using PaperTrading.Domain.Exceptions;
using PaperTrading.Domain.Rules;

namespace PaperTrading.Domain.Entities;

public class Instrument
{
    public int Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Sector { get; private set; } = string.Empty;
    public string Exchange { get; private set; } = "NSE";
    public decimal TickSize { get; private set; } = PriceRules.DefaultTickSize;
    public int LotSize { get; private set; } = 1;
    public decimal PreviousClose { get; private set; }
    public bool IsActive { get; private set; } = true;

    // EF Core parameterless constructor
    private Instrument()
    {
    }

    public Instrument(
        string symbol,
        string name,
        string sector,
        decimal previousClose,
        string exchange = "NSE",
        decimal tickSize = PriceRules.DefaultTickSize,
        int lotSize = 1,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new InvalidPriceException("Symbol cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidPriceException("Name cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(sector))
        {
            throw new InvalidPriceException("Sector cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(exchange))
        {
            throw new InvalidPriceException("Exchange cannot be empty.");
        }

        if (tickSize <= 0)
        {
            throw new InvalidPriceException("Tick size must be greater than zero.");
        }

        if (lotSize <= 0)
        {
            throw new InvalidPriceException("Lot size must be greater than zero.");
        }

        if (previousClose <= 0)
        {
            throw new InvalidPriceException("Previous close must be greater than zero.");
        }

        Symbol = symbol.Trim().ToUpperInvariant();
        Name = name.Trim();
        Sector = sector.Trim();
        Exchange = exchange.Trim().ToUpperInvariant();
        TickSize = tickSize;
        LotSize = lotSize;
        PreviousClose = PriceRules.RoundToTick(previousClose, tickSize);
        IsActive = isActive;
    }

    public void UpdatePreviousClose(decimal newPreviousClose)
    {
        if (newPreviousClose <= 0)
        {
            throw new InvalidPriceException("Previous close must be greater than zero.");
        }

        PreviousClose = PriceRules.RoundToTick(newPreviousClose, TickSize);
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
