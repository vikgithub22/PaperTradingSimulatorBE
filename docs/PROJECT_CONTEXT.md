# Paper Trading Simulator: Project Context

Read this whole file before doing anything. It is the single source of truth for what we
are building, how it is structured, what is already done, and how you must work.

---

## 1. What we are building

A **paper trading app for the Indian stock market** (NSE-style symbols, currency ₹).
Users register and receive virtual money (₹10,00,000). They place market, limit and
stop-loss orders, and see a live portfolio with P&L, trade history and a leaderboard.
Prices stream in real time over SignalR. A background matching engine fills or triggers
pending orders. No real money and no real broker.

Goals: real backend depth (matching engine, concurrency, transactions), real-time UX,
and a dashboard that demos well in two minutes. This is a portfolio project, so code
and design decisions must be production-style and defensible in a technical interview.

## 2. Tech stack (fixed, do not substitute)

| Area | Choice |
|---|---|
| Frontend | Angular 22 (standalone components, signals, new control flow, lazy-loaded feature routes), SCSS, Angular Material, TradingView Lightweight Charts, @microsoft/signalr |
| Backend | .NET 10 Web API, C#, MediatR (CQRS-lite), FluentValidation, EF Core 10 (Dapper only for heavy read queries such as the leaderboard), Serilog |
| Real-time | SignalR, with groups per user and per symbol |
| Background work | BackgroundService + System.Threading.Channels (price feed and matching engine). No external broker. |
| Database | SQL Server, installed locally (no Docker for the DB) |
| Auth | ASP.NET Core Identity + JWT access token + refresh tokens |
| Testing | xUnit and FluentAssertions / AwesomeAssertions. The matching engine and money logic must be well tested. Vitest for Angular tests. |
| DevOps (later) | GitHub Actions CI, optional Docker for API and UI only |

## 3. Architecture

Modular monolith with Clean Architecture. Dependency rule, strictly enforced by project
references:

```
Domain  <-  Application  <-  Infrastructure  <-  API
```

- `PaperTrading.Domain`: entities, value objects, domain rules. **No dependencies.**
- `PaperTrading.Application`: use cases (MediatR commands and queries), interfaces, DTOs, validators. Depends **only on Domain**. Must not reference EF Core.
- `PaperTrading.Infrastructure`: EF Core (`AppDbContext`), configurations, migrations, repositories, price provider, identity and token services.
- `PaperTrading.API`: controllers, SignalR hubs, background services, DI composition root.
- `PaperTrading.Tests`: unit and integration tests.
- `web/`: the Angular app, with a feature-based folder structure and lazy-loaded routes.

### Repository layout

```
PaperTradingSimulator/               (repo root)
  PaperTrading.slnx
  Directory.Build.props              (net10.0, Nullable, ImplicitUsings, LangVersion latest, TreatWarningsAsErrors)
  global.json                        (SDK 10.0.100, rollForward latestFeature)
  dotnet-tools.json                  (local tool manifest: dotnet-ef 10.0.14)
  .gitignore
  README.md
  docs/
    PROJECT_CONTEXT.md
    UI_UX_DESIGN.md
    PHASE_1_PROMPT.md
  src/
    PaperTrading.Domain/
    PaperTrading.Application/
    PaperTrading.Infrastructure/
      Persistence/AppDbContext.cs
      Persistence/Migrations/
      DependencyInjection.cs
    PaperTrading.API/
      Controllers/
      Program.cs
      appsettings.json / appsettings.Development.json
  tests/
    PaperTrading.Tests/
  web/                               (Angular 22 app: SCSS, routing, no SSR)
```

## 4. Key design decisions (already made)

- Orders fill against the **market price** (a simulator), not against other users' orders. An internal order book is an optional v2 stretch goal.
- Prices come from a **simulated feed** (random walk or geometric Brownian motion) seeded with realistic NSE symbols (RELIANCE, TCS, INFY, HDFCBANK and so on), hidden behind an `IPriceProvider` interface so a real API can be plugged in later.
- All money and prices are `decimal`, never `double` or `float`. Store as `decimal(18,2)` or `decimal(18,4)` as appropriate.
- Wallet changes are recorded as **immutable LedgerEntries**. A balance is never changed without a ledger entry.
- Use **optimistic concurrency (rowversion)** and **DB transactions** wherever balances or holdings change.
- The matching engine must be **idempotent**: an order can never be filled twice.

## 5. Domain rules

**Order types**
- Market: fills immediately at the current price (optional small simulated slippage).
- Limit: BUY fills when price <= limit. SELL fills when price >= limit.
- Stop-loss: triggers when the price crosses the trigger, then executes as a market order. Stop-loss applies to existing holdings (sell side).

**Order lifecycle:** Pending -> (PartiallyFilled, optional) -> Filled / Cancelled / Rejected / Expired.

**Reservation**
- BUY: reserve the required cash (qty x price + charges) when the order is placed. Release on cancel or reject.
- SELL: reserve shares. No short selling in v1.

**Holdings:** quantity and average buy price. Realized P&L on sells, unrealized P&L from the live price.

**Indian-market flavour**
- Currency ₹, displayed with Indian digit grouping (en-IN, for example ₹10,00,000).
- Tick size 0.05. Whole-share quantities.
- Market hours 9:15 to 15:30 IST, Mon to Fri, with a **dev/demo toggle** that keeps the simulated market always open.
- Simple, configurable brokerage and charges model (may be zero in v1).

**Leaderboard:** ranked by portfolio value and total return %, computed with SQL window functions, with periodic snapshots.

**Core tables (initial):** Users (Identity), Wallets, LedgerEntries, Instruments, PriceTicks/Candles, Orders, Trades, Holdings, LeaderboardSnapshots, RefreshTokens.

## 6. Build plan: 8 phases plus setup

We build **vertical slices**. Inside each phase the order is **database -> backend -> Angular -> tests -> commit**.

| Phase | Scope | Status |
|---|---|---|
| **0** | Solution setup, Clean Architecture projects, EF Core with local SQL Server, Angular 22 app, Git, basic README | **DONE** |
| **1** | Auth (register, login, JWT, refresh) and the user wallet with the starting balance recorded as a ledger entry. Angular: register, login, auth guard, token interceptor. | **DONE** |
| **2** | Instruments and the simulated price feed, SignalR price streaming. Angular: live ticker. | **In progress** |
| **3** | Market orders: reserve, fill, update holdings and cash in one transaction. Angular: order panel and basic order list. | Not started |
| **4** | Limit and stop-loss orders and the Channels-based matching engine. Angular: pending orders and cancel. | Not started |
| **5** | Portfolio: holdings, average cost, realized and unrealized P&L. Angular: portfolio page. | Not started |
| **6** | Live dashboard: candlestick charts, order panel, notifications over SignalR, real-time order and position updates. | Not started |
| **7** | Leaderboard (SQL window functions, Dapper) and trade history with pagination, filters and indexing. | Not started |
| **8** | Unit tests (matching engine, P&L), load simulation, GitHub Actions CI, final README with architecture diagram. | Not started |

### What Phase 0 delivered

- Solution with five projects and Clean Architecture project references, built on .NET 10.
- `Directory.Build.props` with `TreatWarningsAsErrors=true`.
- EF Core 10.0.14: `AppDbContext`, SQL Server provider, `Microsoft.EntityFrameworkCore.Design`.
- Connection string `DefaultConnection` targeting `PaperTrading` on local SQL Server.
- API with OpenAPI and Swagger UI.
- Angular app scaffolded in `web/` (SCSS, routing, no SSR).
- `README.md` and Conventional Commits history.

### What Phase 1 delivered

- **Domain:** `Wallet` and `LedgerEntry` entities with strict monetary invariants:
  - Private setters enforcing balance mutations only via `Credit()` and `Debit()`.
  - Non-negative balance check, positive amounts, strict 2-decimal-place checks (rejected, not rounded).
  - Invariant that `Balance == sum(LedgerEntries)` verified across random operations.
- **Application:**
  - `Result<T>` and strongly typed `Error` hierarchy.
  - MediatR commands, queries, and validators with pipeline validation behavior.
  - `IUnitOfWork` transaction execution abstraction (`ExecuteInTransactionAsync`).
  - `IIdentityService`, `ITokenService`, `IWalletRepository` abstractions.
  - Handlers: `RegisterCommand` (atomic registration + initial wallet funding), `LoginCommand`, `RefreshTokenCommand`, `LogoutCommand`, `GetCurrentUserQuery`, `GetWalletQuery`, `GetLedgerHistoryQuery`.
- **Infrastructure:**
  - ASP.NET Core Identity with `ApplicationUser : IdentityUser<Guid>` (`DisplayName`, `CreatedAtUtc`).
  - `AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`.
  - Entity configurations: `Wallets`, `LedgerEntries`, `RefreshTokens`.
  - Filtered unique index on `LedgerEntries(WalletId) WHERE Reason = InitialDeposit` for idempotent wallet creation.
  - Paging index on `LedgerEntries(WalletId, CreatedAtUtc DESC, Id DESC)`.
  - Immutable ledger guard in `AppDbContext.SaveChanges` (throws on modify or delete).
  - Refresh token rotation with SHA-256 hash storage, family revocation on replay token reuse, and rowversion concurrency.
  - Migrations: `AddIdentity`, `AddWalletAndLedger`, `AddRefreshTokensAndLedgerGuards`.
- **API:**
  - `AuthController` and `WalletController` returning standard RFC 9457 ProblemDetails.
  - Global `IExceptionHandler` mapping failures without leaking internal exceptions.
  - Serilog structured logging and correlation ID tracking (no tokens or passwords logged).
  - Fixed-window rate limiting on `register`, `login`, and `refresh`.
  - OpenAPI Bearer authorization security scheme configuration.
- **Web (Angular 22):**
  - Dark trading terminal aesthetic (PaperDesk design system) with CSS tokens and responsive layout.
  - In-memory JWT access token management and HttpOnly refresh cookie integration (`proxy.conf.json`).
  - Single-flight refresh token interceptor with automatic retry on 401.
  - Route guards: `authGuard` and `guestGuard`.
  - Login page, registration page with live password rule verification, and authenticated Dashboard displaying cash balance and server-paginated ledger entries.
  - Demo market board with simulated ticking quotes.
- **Testing:**
  - 25 backend integration tests (`PaperTrading.Tests`) running against `PaperTrading_Tests` database.
  - 5 Vitest Angular unit tests covering auth service, interceptor, and dashboard.

## 7. How you must work (rules)

1. **One phase at a time, and within a phase one small step at a time.** Do not start a phase, or add anything from a later phase, until I give you that phase's prompt. If a phase prompt is ambiguous, ask before building.
2. **Do not widen scope.** Build only what the current step asks for. If you think something extra is needed, list it as a suggestion and wait for my answer.
3. **Respect the architecture.** Domain has no dependencies. Application depends only on Domain and must not reference EF Core or ASP.NET. Infrastructure and API sit outside. Never add a project reference that breaks the dependency rule.
4. **Production-style code.** Validation (FluentValidation), error handling, structured logging (Serilog), `CancellationToken` on async paths, async all the way, nullable reference types respected, no magic numbers for money logic.
5. **Money is `decimal`.** Wallet changes only through immutable ledger entries. Balance or holding changes use transactions and rowversion concurrency.
6. **Tests for core logic.** The matching engine, money and P&L calculations must have unit tests. Tests must pass before a step is considered done.
7. **The build must stay clean:** `dotnet build` with 0 warnings and 0 errors, and `dotnet test` green. For the Angular app, `npm run build` must pass. Never suppress warnings or disable `TreatWarningsAsErrors` to get green.
8. **Packages:** prefer what is already in the stack. State every new NuGet or npm package and why it is needed before adding it. If a package version or API has changed, say so.
9. **Migrations:** one named migration per schema change, using the command pattern above. Never edit an applied migration. Create a new one.
10. **Commits:** Conventional Commits (`feat(auth): ...`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`), one commit per completed step. Do not commit unless I ask, or the step says so.
11. **Secrets:** never commit secrets. Use user-secrets or environment variables for signing keys and similar values. Only the dev SQL connection string using Windows authentication may live in `appsettings.Development.json`.
12. **At the end of each step**, report briefly: what you changed (files), the design decisions and trade-offs, how to verify it (exact commands and expected output), and anything I should know or decide. I will explain these decisions in interviews, so make the reasoning clear.
13. **When something fails**, show the exact error and your diagnosis. Do not hide it or work around it by weakening checks.
14. If I ask for something wrong or risky, say so directly and explain the consequences before proceeding.
