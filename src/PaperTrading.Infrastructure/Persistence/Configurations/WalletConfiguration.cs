using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .ValueGeneratedNever();

        builder.Property(w => w.UserId)
            .IsRequired();

        builder.Property(w => w.Balance)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(w => w.RowVersion)
            .IsRowVersion()
            .IsRequired();

        builder.Property(w => w.CreatedAtUtc)
            .IsRequired();

        builder.HasMany(w => w.LedgerEntries)
            .WithOne()
            .HasForeignKey(e => e.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.UserId)
            .IsUnique();
    }
}
