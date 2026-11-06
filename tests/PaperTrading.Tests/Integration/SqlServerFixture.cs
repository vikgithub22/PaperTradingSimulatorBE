using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PaperTrading.Infrastructure.Persistence;

namespace PaperTrading.Tests.Integration;

public sealed class SqlServerFixture : IAsyncLifetime
{
    public string ConnectionString { get; } =
        "Server=localhost;Database=PaperTrading_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    public async Task InitializeAsync()
    {
        await ResetAsync();
    }

    public async Task DisposeAsync()
    {
        SqlConnection.ClearAllPools();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
    }

    private async Task ResetAsync()
    {
        SqlConnection.ClearAllPools();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "Integration";
}
