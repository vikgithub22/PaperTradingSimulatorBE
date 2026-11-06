using System.Net;
using System.Net.Http.Json;

namespace PaperTrading.Tests.Integration;

[Collection(IntegrationCollection.Name)]
public sealed class ConcurrencyAndRateLimitTests : IDisposable
{
    private readonly PaperTradingApiFactory _factory;
    private readonly PaperTradingApiFactory _rateLimitedFactory;

    public ConcurrencyAndRateLimitTests(SqlServerFixture database)
    {
        _factory = new PaperTradingApiFactory(database.ConnectionString);
        _rateLimitedFactory = new PaperTradingApiFactory(database.ConnectionString, authPermitLimit: 5);
    }

    [Fact]
    public async Task TwoSimultaneousRegistrations_ProduceOneCreatedAndOneConflict()
    {
        using var client = _factory.CreateClient();
        var email = $"race-{Guid.NewGuid():N}@example.com";
        var body = new { email, password = "Password1", displayName = "Race User" };

        var responses = await Task.WhenAll(
            client.PostAsJsonAsync("/api/auth/register", body),
            client.PostAsJsonAsync("/api/auth/register", body));

        var codes = responses.Select(response => response.StatusCode).OrderBy(code => code).ToArray();
        codes.Should().Equal(HttpStatusCode.Created, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task TwoSimultaneousRefreshes_ProduceExactlyOneSuccess()
    {
        using var client = _factory.CreateClient(new() { HandleCookies = false });
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "Password1",
            displayName = "Refresh User"
        });
        registered.StatusCode.Should().Be(HttpStatusCode.Created);
        var cookie = registered.Headers.GetValues("Set-Cookie")
            .First(value => value.StartsWith("refreshToken=", StringComparison.OrdinalIgnoreCase))
            .Split(';', 2)[0]
            .Split('=', 2)[1];

        var first = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        first.Headers.Add("Cookie", $"refreshToken={cookie}");
        var second = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        second.Headers.Add("Cookie", $"refreshToken={cookie}");

        var responses = await Task.WhenAll(client.SendAsync(first), client.SendAsync(second));
        responses.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        responses.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);
    }

    [Fact]
    public async Task HammeringLogin_Returns429()
    {
        using var client = _rateLimitedFactory.CreateClient();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last = await client.PostAsJsonAsync("/api/auth/login", new
            {
                email = "nobody@example.com",
                password = "Password1"
            });
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await last.Content.ReadAsStringAsync()).Should().Contain("Rate limit exceeded");
    }

    public void Dispose()
    {
        _factory.Dispose();
        _rateLimitedFactory.Dispose();
    }
}
