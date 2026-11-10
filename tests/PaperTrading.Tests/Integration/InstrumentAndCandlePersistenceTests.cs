using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PaperTrading.Domain.Entities;
using PaperTrading.Infrastructure.Persistence;

namespace PaperTrading.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public sealed class InstrumentAndCandlePersistenceTests(SqlServerFixture fixture)
{
    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task Instruments_AreSeeded_WithExpectedLargeCaps()
    {
        await using var db = CreateDbContext();

        var instruments = await db.Instruments.AsNoTracking().ToListAsync();

        instruments.Should().HaveCount(20);
        instruments.Select(i => i.Symbol).Should().Contain(["RELIANCE", "TCS", "HDFCBANK", "INFY", "ITC", "SBIN"]);
        instruments.Should().OnlyContain(i => i.TickSize == 0.05m && i.PreviousClose > 0 && i.Exchange == "NSE");
    }

    [Fact]
    public async Task Database_RejectsDuplicateCandle_OnInstrumentAndOpenTimeUtc()
    {
        await using var db = CreateDbContext();

        var instrument = await db.Instruments.FirstAsync();
        var openTimeUtc = new DateTime(2026, 11, 8, 9, 15, 0, DateTimeKind.Utc);

        var candle1 = new Candle(
            instrumentId: instrument.Id,
            openTimeUtc: openTimeUtc,
            open: 100m,
            high: 105m,
            low: 95m,
            close: 102m,
            volume: 500);

        db.Candles.Add(candle1);
        await db.SaveChangesAsync();

        var candle2 = new Candle(
            instrumentId: instrument.Id,
            openTimeUtc: openTimeUtc, // same instrument and timestamp
            open: 102m,
            high: 106m,
            low: 101m,
            close: 104m,
            volume: 750);

        db.Candles.Add(candle2);

        var act = () => db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithInnerException<DbUpdateException, SqlException>()
            .Where(ex => ex.Message.Contains("IX_Candles_InstrumentId_OpenTimeUtc") || ex.Number == 2601 || ex.Number == 2627);
    }

    [Fact]
    public async Task Database_RejectsInvalidOhlcRow_ThroughCheckConstraint()
    {
        await using var db = CreateDbContext();
        var instrument = await db.Instruments.FirstAsync();

        // Direct SQL execution to bypass C# domain constructor checks and directly test database CHECK constraints
        var invalidSql = $@"
            INSERT INTO Candles (Id, InstrumentId, OpenTimeUtc, [Open], [High], [Low], [Close], [Volume])
            VALUES ('{Guid.CreateVersion7()}', {instrument.Id}, SYSUTCDATETIME(), 100.00, 90.00, 105.00, 95.00, 1000)"; // High (90) < Low (105)

        var act = () => db.Database.ExecuteSqlRawAsync(invalidSql);

        await act.Should().ThrowAsync<SqlException>()
            .Where(ex => ex.Message.Contains("CK_Candles_High_Gte_Low") || ex.Number == 547);
    }
}
