namespace PaperTrading.API.Auth;

public static class RefreshCookie
{
    public const string Name = "refreshToken";
    public const string Path = "/api/auth";

    public static void Append(HttpResponse response, string token, DateTimeOffset expires, bool secure)
    {
        response.Cookies.Append(Name, token, Options(secure, expires));
    }

    public static void Clear(HttpResponse response, bool secure)
    {
        response.Cookies.Delete(Name, Options(secure, expires: null));
    }

    private static CookieOptions Options(bool secure, DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = secure,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
