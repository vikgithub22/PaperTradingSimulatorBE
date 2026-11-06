using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("LedgerEntries", table =>
        {
            table.HasCheckConstraint("CK_LedgerEntries_Amount_Positive", "[Amount] > 0");
        });

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

        // Reason value 1 is LedgerEntryReason.InitialDeposit. One starting balance per wallet.
        builder.HasIndex(e => e.WalletId)
            .IsUnique()
            .HasFilter("[Reason] = 1")
            .HasDatabaseName("IX_LedgerEntries_OneInitialDeposit");

        builder.HasIndex(e => new { e.WalletId, e.CreatedAtUtc, e.Id })
            .IsDescending(false, true, true)
            .HasDatabaseName("IX_LedgerEntries_WalletId_CreatedAtUtc_Id");
    }
}
