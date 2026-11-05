namespace PaperTrading.Application.Configuration;

public sealed class WalletOptions
{
    public const string SectionName = "Wallet";

    public decimal StartingBalance { get; set; } = 1_000_000m;
}
