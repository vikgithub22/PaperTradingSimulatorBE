using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PaperTrading.Application.Common.Interfaces;
using PaperTrading.Domain.Entities;

namespace PaperTrading.Tests.Integration;

public sealed class PaperTradingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;
    private readonly int _authPermitLimit;
    private readonly bool _failWalletWrites;

    public PaperTradingApiFactory(string connectionString, int authPermitLimit = 1000, bool failWalletWrites = false)
    {
        _connectionString = connectionString;
        _authPermitLimit = authPermitLimit;
        _failWalletWrites = failWalletWrites;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", _connectionString);
        builder.UseSetting("Jwt:Issuer", "PaperTrading.Tests");
        builder.UseSetting("Jwt:Audience", "PaperTrading.Tests");
        builder.UseSetting("Jwt:SigningKey", "test-signing-key-must-be-at-least-32-bytes");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Wallet:StartingBalance", "1000000");
        builder.UseSetting(
            "RateLimiting:AuthPermitLimit",
            _authPermitLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.UseSetting("RateLimiting:AuthWindowSeconds", "60");

        if (_failWalletWrites)
        {
            builder.ConfigureTestServices(services =>
            {
                var descriptor = services.Single(service => service.ServiceType == typeof(IWalletRepository));
                services.Remove(descriptor);
                services.AddScoped<IWalletRepository, ThrowingWalletRepository>();
            });
        }
    }

    private sealed class ThrowingWalletRepository : IWalletRepository
    {
        public Task<Wallet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Forced wallet failure.");

        public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Forced wallet failure.");

        public Task AddAsync(Wallet wallet, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Forced wallet failure.");

        public Task<(IReadOnlyList<LedgerEntry> Items, int TotalCount)> GetLedgerEntriesPagedAsync(
            Guid walletId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Forced wallet failure.");
    }
}
