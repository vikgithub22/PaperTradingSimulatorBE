using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaperTrading.API.Auth;
using PaperTrading.Domain.Enums;
using PaperTrading.Infrastructure.Persistence;

namespace PaperTrading.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public sealed class AuthAndWalletTests : IDisposable
{
    private const string Password = "Password1";
    private readonly SqlServerFixture _database;
    private readonly PaperTradingApiFactory _factory;
    private readonly HttpClient _client;

    public AuthAndWalletTests(SqlServerFixture database)
    {
        _database = database;
        _factory = new PaperTradingApiFactory(database.ConnectionString);
        _client = _factory.CreateClient(new() { HandleCookies = false });
    }

    [Fact]
    public async Task Register_CreatesUserWalletAndStartingLedgerEntry()
    {
        var email = NewEmail();
        var response = await RegisterAsync(email);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("user").GetProperty("email").GetString().Should().Be(email);
        Cookie(response).Should().NotBeNullOrWhiteSpace();
        CookieHeader(response).Should().Contain("httponly");
        CookieHeader(response).Should().Contain("samesite=strict");
        CookieHeader(response).Should().Contain("path=/api/auth");

        await using var db = NewDb();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var wallet = await db.Wallets.SingleAsync(w => w.UserId == user.Id);
        wallet.Balance.Should().Be(1_000_000.00m);
        var entries = await db.LedgerEntries.Where(e => e.WalletId == wallet.Id).ToListAsync();
        entries.Should().ContainSingle();
        entries[0].Reason.Should().Be(LedgerEntryReason.InitialDeposit);
        entries[0].Amount.Should().Be(1_000_000.00m);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = NewEmail();
        (await RegisterAsync(email)).StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await RegisterAsync(email);
        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_WhenWalletCreationFails_LeavesNoUser()
    {
        using var factory = new PaperTradingApiFactory(_database.ConnectionString, failWalletWrites: true);
        using var client = factory.CreateClient();
        var email = NewEmail();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            displayName = "Test User"
        });

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        await using var db = NewDb();
        (await db.Users.AnyAsync(u => u.Email == email)).Should().BeFalse();
    }

    [Fact]
    public async Task Login_And_Me_RequireAValidAccessToken()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await ReadJsonAsync(login)).GetProperty("accessToken").GetString();

        var anonymous = await _client.GetAsync("/api/auth/me");
        anonymous.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var me = await SendAsync(HttpMethod.Get, "/api/auth/me", token);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(me)).GetProperty("email").GetString().Should().Be(email);
    }

    [Fact]
    public async Task Login_WrongPassword_IsGeneric401_AndFifthFailureLocksTheAccount()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            var wrong = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass1" });
            wrong.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await wrong.Content.ReadAsStringAsync()).Should().Contain("Invalid email or password");
        }

        var locked = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass1" });
        locked.StatusCode.Should().Be(HttpStatusCode.Locked);

        var correct = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = Password });
        correct.StatusCode.Should().Be(HttpStatusCode.Locked);
    }

    [Fact]
    public async Task Refresh_RotatesCookie_AndReuseRevokesTheFamily()
    {
        var registered = await RegisterAsync(NewEmail());
        var original = Cookie(registered)!;

        var first = await RefreshAsync(original);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = Cookie(first)!;
        rotated.Should().NotBe(original);
        (await ReadJsonAsync(first)).GetProperty("accessToken").GetString().Should().NotBeNullOrWhiteSpace();

        var replay = await RefreshAsync(original);
        replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var newer = await RefreshAsync(rotated);
        newer.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_RevokesTheFamilyAndClearsTheCookie()
    {
        var registered = await RegisterAsync(NewEmail());
        var cookie = Cookie(registered)!;

        var logout = await SendAsync(HttpMethod.Post, "/api/auth/logout", cookie: cookie);
        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        CookieHeader(logout).Should().Contain("refreshToken=");

        var refresh = await RefreshAsync(cookie);
        refresh.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wallet_ReturnsStartingBalance_AndPagedLedger()
    {
        var registered = await RegisterAsync(NewEmail());
        var token = (await ReadJsonAsync(registered)).GetProperty("accessToken").GetString();

        var wallet = await SendAsync(HttpMethod.Get, "/api/wallet", token);
        wallet.StatusCode.Should().Be(HttpStatusCode.OK);
        var walletJson = await ReadJsonAsync(wallet);
        walletJson.GetProperty("balance").GetDecimal().Should().Be(1_000_000.00m);
        walletJson.GetProperty("currency").GetString().Should().Be("INR");
        (await wallet.Content.ReadAsStringAsync()).Should().Contain("1000000.00");

        var ledger = await SendAsync(HttpMethod.Get, "/api/wallet/ledger?page=1&pageSize=20", token);
        ledger.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await ReadJsonAsync(ledger);
        page.GetProperty("totalCount").GetInt32().Should().Be(1);
        page.GetProperty("items").GetArrayLength().Should().Be(1);
        page.GetProperty("items")[0].GetProperty("direction").GetString().Should().Be("Credit");
        page.GetProperty("items")[0].GetProperty("amount").GetDecimal().Should().Be(1_000_000.00m);

        var empty = await SendAsync(HttpMethod.Get, "/api/wallet/ledger?page=2&pageSize=20", token);
        (await ReadJsonAsync(empty)).GetProperty("items").GetArrayLength().Should().Be(0);

        var invalid = await SendAsync(HttpMethod.Get, "/api/wallet/ledger?page=0&pageSize=101", token);
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Ledger_CannotBeModifiedOrDeleted_AndInitialDepositIsUnique()
    {
        var email = NewEmail();
        await RegisterAsync(email);

        await using var db = NewDb();
        var user = await db.Users.SingleAsync(u => u.Email == email);
        var wallet = await db.Wallets.Include(w => w.LedgerEntries).SingleAsync(w => w.UserId == user.Id);
        var entry = wallet.LedgerEntries.Single();

        db.Entry(entry).State = EntityState.Modified;
        var modify = async () => await db.SaveChangesAsync();
        await modify.Should().ThrowAsync<InvalidOperationException>();

        db.ChangeTracker.Clear();
        entry = await db.LedgerEntries.SingleAsync(e => e.WalletId == wallet.Id);
        db.LedgerEntries.Remove(entry);
        var delete = async () => await db.SaveChangesAsync();
        await delete.Should().ThrowAsync<InvalidOperationException>();

        db.ChangeTracker.Clear();
        wallet = await db.Wallets.Include(w => w.LedgerEntries).SingleAsync(w => w.Id == wallet.Id);
        wallet.Credit(1.00m, LedgerEntryReason.InitialDeposit, "second deposit");
        var duplicate = async () => await db.SaveChangesAsync();
        await duplicate.Should().ThrowAsync<DbUpdateException>();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<HttpResponseMessage> RegisterAsync(string email)
        => await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = Password,
            displayName = "Test User"
        });

    private Task<HttpResponseMessage> RefreshAsync(string cookie)
        => SendAsync(HttpMethod.Post, "/api/auth/refresh", cookie: cookie);

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string url,
        string? bearer = null,
        string? cookie = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (bearer is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
        }

        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"{RefreshCookie.Name}={cookie}");
        }

        return await _client.SendAsync(request);
    }

    private AppDbContext NewDb()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_database.ConnectionString).Options);

    private static string NewEmail() => $"user-{Guid.NewGuid():N}@example.com";

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return document.RootElement.Clone();
    }

    private static string? Cookie(HttpResponseMessage response)
    {
        var header = CookieHeader(response);
        if (header is null)
        {
            return null;
        }

        var pair = header.Split(';', 2)[0];
        var separator = pair.IndexOf('=');
        return separator < 0 ? null : pair[(separator + 1)..];
    }

    private static string? CookieHeader(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        return values.FirstOrDefault(value => value.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase));
    }
}
