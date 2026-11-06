# Paper Trading Simulator

A paper-trading app for the Indian market. Users register, receive ₹10,00,000 of virtual cash, and see that starting balance as one immutable ledger entry. No real money and no broker.

## Architecture

```
Domain  <-  Application  <-  Infrastructure  <-  API
web/    Angular 22
```

Money changes only through ledger entries. The access token stays in memory. The refresh token is an HttpOnly cookie (`Path=/api/auth`, SameSite=Strict). It is stored as a SHA-256 hash, rotated on every use, and the whole token family is revoked if an old token is replayed.

In Development and Testing the cookie is not marked Secure, so the HTTP Angular dev server can keep the session. Production marks it Secure.

## Run the API

```powershell
dotnet user-secrets set "Jwt:SigningKey" "<at least 32 characters>" --project src/PaperTrading.API
dotnet run --project src/PaperTrading.API --launch-profile https
```

Swagger: https://localhost:7200/swagger

SQL Server database `PaperTrading`, Windows auth. Connection string is in `src/PaperTrading.API/appsettings.Development.json`. Apply migrations with:

```powershell
dotnet tool restore
dotnet ef database update --project src/PaperTrading.Infrastructure --startup-project src/PaperTrading.API
```

## Run the Angular app

```powershell
cd web
npm install
npm start
```

Open http://localhost:4200. The dev server proxies `/api` to https://localhost:7200.

## Tests

```powershell
dotnet test
cd web
npm test
```

`dotnet test` uses a separate database, `PaperTrading_Tests`, and drops it when the run finishes.

## Known limitation

Two browser tabs can refresh at the same time. Reuse of an already-rotated token revokes the whole family, so one of those tabs can log the user out. A single tab uses one in-flight refresh.

## Roadmap

| Phase | Scope | Status |
|---|---|---|
| 0 | Solution, EF Core, Angular scaffold | Done |
| 1 | Auth, wallet, ledger, login and dashboard | Done |
| 2 | Instruments and simulated prices | Not started |
| 3 | Market orders | Not started |
| 4 | Limit and stop-loss orders | Not started |
| 5 | Portfolio and P&L | Not started |
| 6 | Live dashboard | Not started |
| 7 | Leaderboard and trade history | Not started |
| 8 | Tests, load simulation, CI | Not started |
