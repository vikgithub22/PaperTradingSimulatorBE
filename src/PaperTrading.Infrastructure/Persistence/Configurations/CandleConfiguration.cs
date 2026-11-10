using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

internal sealed class CandleConfiguration : IEntityTypeConfiguration<Candle>
{
    public void Configure(EntityTypeBuilder<Candle> builder)
    {
        builder.ToTable("Candles", table =>
        {
            table.HasCheckConstraint("CK_Candles_High_Gte_Low", "[High] >= [Low]");
            table.HasCheckConstraint("CK_Candles_High_Gte_Open", "[High] >= [Open]");
            table.HasCheckConstraint("CK_Candles_High_Gte_Close", "[High] >= [Close]");
            table.HasCheckConstraint("CK_Candles_Low_Lte_Open", "[Low] <= [Open]");
            table.HasCheckConstraint("CK_Candles_Low_Lte_Close", "[Low] <= [Close]");
            table.HasCheckConstraint("CK_Candles_Low_Positive", "[Low] > 0");
            table.HasCheckConstraint("CK_Candles_Volume_NonNegative", "[Volume] >= 0");
        });

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.InstrumentId)
            .IsRequired();

        builder.Property(e => e.OpenTimeUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(e => e.Open)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.High)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Low)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Close)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Volume)
            .IsRequired();

        // Foreign key to Instrument
        builder.HasOne<Instrument>()
            .WithMany()
            .HasForeignKey(e => e.InstrumentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on (InstrumentId, OpenTimeUtc) for idempotent minute candle upserts
        builder.HasIndex(e => new { e.InstrumentId, e.OpenTimeUtc })
            .IsUnique()
            .HasDatabaseName("IX_Candles_InstrumentId_OpenTimeUtc");
    }
}
