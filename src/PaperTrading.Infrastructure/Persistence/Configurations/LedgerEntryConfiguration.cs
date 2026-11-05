using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.WalletId)
            .IsRequired();

        builder.Property(e => e.Direction)
            .IsRequired();

        builder.Property(e => e.Reason)
            .IsRequired();

        builder.Property(e => e.Amount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.BalanceAfter)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Reference)
            .HasMaxLength(64);

        builder.Property(e => e.Description)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(e => e.WalletId);
    }
}
