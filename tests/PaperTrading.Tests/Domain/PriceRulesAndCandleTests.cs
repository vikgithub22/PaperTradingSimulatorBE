using FluentAssertions;
using PaperTrading.Domain.Entities;
using PaperTrading.Domain.Exceptions;
using PaperTrading.Domain.Rules;

namespace PaperTrading.Tests.Domain;

public sealed class PriceRulesAndCandleTests
{
    [Theory]
    [InlineData(100.024, 100.00)]
    [InlineData(100.025, 100.05)] // Midpoint rounds away from zero
    [InlineData(100.026, 100.05)]
    [InlineData(100.074, 100.05)]
    [InlineData(100.075, 100.10)] // Midpoint rounds away from zero
    [InlineData(0.025, 0.05)]
    public void RoundToTick_RoundsToNearestTickWithAwayFromZeroAtMidpoint(decimal input, decimal expected)
    {
        var result = PriceRules.RoundToTick(input, 0.05m);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(100.00, true)]
    [InlineData(100.05, true)]
    [InlineData(100.10, true)]
    [InlineData(100.02, false)]
    [InlineData(100.025, false)]
    [InlineData(0.00, false)]
    [InlineData(-10.05, false)]
    public void IsOnTickGrid_IdentifiesValidAndInvalidTicks(decimal input, bool expected)
    {
        var result = PriceRules.IsOnTickGrid(input, 0.05m);
        result.Should().Be(expected);
    }

    [Fact]
    public void ApplyDailyBand_ClampsWithinPlusMinusTenPercent()
    {
        const decimal previousClose = 1000.00m;
        // Upper band: 1100.00, Lower band: 900.00

        // Inside band
        PriceRules.ApplyDailyBand(1050.00m, previousClose).Should().Be(1050.00m);
        PriceRules.ApplyDailyBand(950.00m, previousClose).Should().Be(950.00m);

        // Above upper band
        PriceRules.ApplyDailyBand(1200.00m, previousClose).Should().Be(1100.00m);

        // Below lower band
        PriceRules.ApplyDailyBand(800.00m, previousClose).Should().Be(900.00m);
    }

    [Fact]
    public void EnsureMinTick_NeverAllowsPriceBelowOneTick()
    {
        PriceRules.EnsureMinTick(0.00m, 0.05m).Should().Be(0.05m);
        PriceRules.EnsureMinTick(-10.50m, 0.05m).Should().Be(0.05m);
        PriceRules.EnsureMinTick(0.02m, 0.05m).Should().Be(0.05m);
        PriceRules.EnsureMinTick(100.00m, 0.05m).Should().Be(100.00m);
    }

    [Fact]
    public void ApplyDailyBand_NeverDropsBelowOneTickEvenWithSmallPreviousClose()
    {
        const decimal previousClose = 0.05m;
        var result = PriceRules.ApplyDailyBand(0.01m, previousClose, bandPercent: 10m, tickSize: 0.05m);
        result.Should().Be(0.05m);
    }

    [Fact]
    public void Candle_WithValidOhlc_InstantiatesSuccessfully()
    {
        var openTime = DateTime.UtcNow;
        var candle = new Candle(
            instrumentId: 1,
            openTimeUtc: openTime,
            open: 100.00m,
            high: 105.00m,
            low: 98.00m,
            close: 102.50m,
            volume: 1500);

        candle.Id.Should().NotBeEmpty();
        candle.InstrumentId.Should().Be(1);
        candle.OpenTimeUtc.Should().Be(DateTime.SpecifyKind(openTime, DateTimeKind.Utc));
        candle.Open.Should().Be(100.00m);
        candle.High.Should().Be(105.00m);
        candle.Low.Should().Be(98.00m);
        candle.Close.Should().Be(102.50m);
        candle.Volume.Should().Be(1500);
    }

    [Theory]
    [InlineData(100, 95, 90, 92, "High < Open")]
    [InlineData(90, 95, 100, 92, "Low > Open")]
    [InlineData(95, 90, 100, 92, "High < Low")]
    [InlineData(95, 98, 90, 100, "High < Close")]
    [InlineData(95, 100, 96, 92, "Low > Close")]
    [InlineData(95, 100, 0, 95, "Low <= 0")]
    public void Candle_WithInvalidOhlcInvariants_ThrowsInvalidCandleException(
        decimal open, decimal high, decimal low, decimal close, string reason)
    {
        var act = () => new Candle(
            instrumentId: 1,
            openTimeUtc: DateTime.UtcNow,
            open: open,
            high: high,
            low: low,
            close: close,
            volume: 100);

        act.Should().Throw<InvalidCandleException>(reason);
    }

    [Fact]
    public void Candle_WithNegativeVolume_ThrowsInvalidCandleException()
    {
        var act = () => new Candle(
            instrumentId: 1,
            openTimeUtc: DateTime.UtcNow,
            open: 100m,
            high: 105m,
            low: 95m,
            close: 100m,
            volume: -10);

        act.Should().Throw<InvalidCandleException>();
    }

    [Fact]
    public void Instrument_WithValidParameters_InitializesCorrectly()
    {
        var instrument = new Instrument(
            symbol: "reliance",
            name: "Reliance Industries Ltd",
            sector: "Energy",
            previousClose: 2450.03m);

        instrument.Symbol.Should().Be("RELIANCE");
        instrument.Name.Should().Be("Reliance Industries Ltd");
        instrument.Sector.Should().Be("Energy");
        instrument.Exchange.Should().Be("NSE");
        instrument.TickSize.Should().Be(0.05m);
        instrument.LotSize.Should().Be(1);
        instrument.PreviousClose.Should().Be(2450.05m); // Rounded to 0.05
        instrument.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Instrument_UpdatePreviousClose_RoundsToTick()
    {
        var instrument = new Instrument("TCS", "Tata Consultancy Services", "IT", 3500.00m);
        instrument.UpdatePreviousClose(3520.02m);

        instrument.PreviousClose.Should().Be(3520.00m);
    }
}
