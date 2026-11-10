using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

internal sealed class InstrumentConfiguration : IEntityTypeConfiguration<Instrument>
{
    public void Configure(EntityTypeBuilder<Instrument> builder)
    {
        builder.ToTable("Instruments");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Symbol)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Sector)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Exchange)
            .HasMaxLength(10)
            .HasDefaultValue("NSE")
            .IsRequired();

        builder.Property(e => e.TickSize)
            .HasColumnType("decimal(4,2)")
            .HasDefaultValue(0.05m)
            .IsRequired();

        builder.Property(e => e.LotSize)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(e => e.PreviousClose)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(e => e.Symbol)
            .IsUnique()
            .HasDatabaseName("IX_Instruments_Symbol");

        // Seed about 20 NSE large cap instruments.
        // NOTE: The PreviousClose values are fictional starting values for a simulation.
        builder.HasData(
            CreateSeedInstrument(1, "RELIANCE", "Reliance Industries Ltd", "Energy", 2450.00m),
            CreateSeedInstrument(2, "TCS", "Tata Consultancy Services Ltd", "Technology", 3500.00m),
            CreateSeedInstrument(3, "HDFCBANK", "HDFC Bank Ltd", "Financial Services", 1650.00m),
            CreateSeedInstrument(4, "ICICIBANK", "ICICI Bank Ltd", "Financial Services", 1100.00m),
            CreateSeedInstrument(5, "INFY", "Infosys Ltd", "Technology", 1550.00m),
            CreateSeedInstrument(6, "BHARTIARTL", "Bharti Airtel Ltd", "Telecommunication", 1300.00m),
            CreateSeedInstrument(7, "ITC", "ITC Ltd", "Consumer Goods", 440.00m),
            CreateSeedInstrument(8, "SBIN", "State Bank of India", "Financial Services", 750.00m),
            CreateSeedInstrument(9, "LT", "Larsen & Toubro Ltd", "Construction", 3600.00m),
            CreateSeedInstrument(10, "HINDUNILVR", "Hindustan Unilever Ltd", "Consumer Goods", 2400.00m),
            CreateSeedInstrument(11, "KOTAKBANK", "Kotak Mahindra Bank Ltd", "Financial Services", 1750.00m),
            CreateSeedInstrument(12, "AXISBANK", "Axis Bank Ltd", "Financial Services", 1150.00m),
            CreateSeedInstrument(13, "BAJFINANCE", "Bajaj Finance Ltd", "Financial Services", 6800.00m),
            CreateSeedInstrument(14, "MARUTI", "Maruti Suzuki India Ltd", "Automobile", 12200.00m),
            CreateSeedInstrument(15, "SUNPHARMA", "Sun Pharmaceutical Industries Ltd", "Healthcare", 1600.00m),
            CreateSeedInstrument(16, "TITAN", "Titan Company Ltd", "Consumer Durables", 3400.00m),
            CreateSeedInstrument(17, "ASIANPAINT", "Asian Paints Ltd", "Consumer Goods", 2850.00m),
            CreateSeedInstrument(18, "WIPRO", "Wipro Ltd", "Technology", 480.00m),
            CreateSeedInstrument(19, "NTPC", "NTPC Ltd", "Utilities", 380.00m),
            CreateSeedInstrument(20, "ONGC", "Oil and Natural Gas Corporation Ltd", "Energy", 260.00m)
        );
    }

    private static object CreateSeedInstrument(
        int id,
        string symbol,
        string name,
        string sector,
        decimal previousClose,
        string exchange = "NSE",
        decimal tickSize = 0.05m,
        int lotSize = 1,
        bool isActive = true)
    {
        return new
        {
            Id = id,
            Symbol = symbol,
            Name = name,
            Sector = sector,
            Exchange = exchange,
            TickSize = tickSize,
            LotSize = lotSize,
            PreviousClose = previousClose,
            IsActive = isActive
        };
    }
}
