# PHASE 1: Authentication, Wallet and Ledger

Context: `docs/PROJECT_CONTEXT.md`. All the working rules in section 7 of that file apply.

## A. Goal of Phase 1

A user can **register**, **log in**, stay logged in via **refresh tokens**, **log out**, and
see their **wallet**: a starting balance of **₹10,00,000** recorded as an **immutable ledger
entry**, with a paged ledger history. End to end: SQL Server tables -> API -> Angular screens.

Out of scope (do NOT build): email confirmation, password reset, roles or admin, external
logins, 2FA, instruments, prices, orders, SignalR, leaderboard, any later-phase feature.

## B. Design decisions for this phase (already made)

**Identity and layering**
- `ApplicationUser : IdentityUser<Guid>` lives in **Infrastructure** (it depends on ASP.NET Identity). Extra columns: `DisplayName` (nvarchar(30), required, 2-30 chars, not unique) and `CreatedAtUtc`.
- Domain and Application know a user only as a `Guid UserId`. No Identity types leak above Infrastructure.
- `AppDbContext` becomes `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`. Keep the default AspNet* table names. Email is the username. No roles are used yet.
- Password policy: min 8 characters, at least one upper-case, one lower-case and one digit. Lockout enabled (5 failed attempts, 5 minutes). The Angular form mirrors these rules.

**Tokens**
- **Access token:** JWT, HS256, 15 minutes. Claims: `sub` (user id), `email`, `jti`. Validate issuer, audience, lifetime and signing key, with 30 seconds of clock skew. The signing key comes from **user-secrets** (minimum 32 bytes), never from a committed file. Options bound via the options pattern (`JwtOptions`).
- **Refresh token:** 64 random bytes (`RandomNumberGenerator`), 7-day absolute lifetime, **rotated on every use**. Only the **SHA-256 hash** is stored. Each token belongs to a `FamilyId`. **Reuse of an already-rotated token revokes the whole family** (theft detection). Rotation is protected by a rowversion so two concurrent refreshes cannot both succeed.
- **Transport:** the refresh token travels in an **HttpOnly, Secure, SameSite=Strict cookie** scoped to `Path=/api/auth`. The access token is returned in the JSON body and held **in memory only** in Angular (never localStorage). In development the Angular dev server **proxies `/api` to the API**, so both appear same-origin and the cookie works.
- Known limitation to document in the README: a multi-tab refresh race can log a user out, because rotated-token reuse revokes the family. In a single tab, Angular uses single-flight refresh.

**Wallet and ledger (money rules)**
- `Wallet`: one per user (unique `UserId`), `Balance decimal(18,2)` with a `CHECK (Balance >= 0)`, `RowVersion`. `Balance` has a **private setter**. It changes only through `Credit(...)` and `Debit(...)`, and each returns the `LedgerEntry` it created. There is no way to change a balance without producing a ledger entry.
- `LedgerEntry` is **immutable**: no update, no delete. Enforce it by overriding `SaveChanges` in `AppDbContext` to throw if a ledger entry is modified or deleted. Columns: `Id` (use `Guid.CreateVersion7()`), `WalletId`, `Direction` (Credit/Debit), `Reason` (enum; now only `InitialDeposit`, later phases add more), `Amount decimal(18,2)` with `CHECK (Amount > 0)`, `BalanceAfter decimal(18,2)`, `Reference nvarchar(64)` null, `Description nvarchar(200)`, `CreatedAtUtc`.
- Amounts must be positive and have at most 2 decimal places. **Reject**, do not round silently.
- **Idempotent starting balance:** a **unique filtered index** on `LedgerEntries(WalletId) WHERE Reason = InitialDeposit` makes it impossible to grant the starting balance twice.
- Index for history paging: `(WalletId, CreatedAtUtc DESC, Id DESC)`.
- Starting balance comes from configuration (`WalletOptions.StartingBalance`, default `1000000m`).
- Inject `TimeProvider`. Never call `DateTime.UtcNow` directly.
- **Registration is atomic:** user, wallet and initial ledger entry are created in **one transaction**. Application defines `IUnitOfWork.ExecuteInTransactionAsync(...)`. Infrastructure implements it using `CreateExecutionStrategy()` so it stays compatible if retries are enabled later.

**API conventions**
- MediatR commands and queries in Application. FluentValidation runs in a **MediatR pipeline behavior**. Do not use ASP.NET auto-validation.
- `Result<T>` with typed errors in Application. The API maps them to **RFC 9457 ProblemDetails**: 400 validation (with an `errors` dictionary), 401, 409 for a duplicate email, 423 or 429 for lockout and rate limit, 500 via a global `IExceptionHandler`. Never leak exception details.
- Login failures return a **generic message** (do not reveal whether the email exists). Registration with an existing email returns 409. Note this enumeration trade-off in the step report.
- Serilog (console and rolling file), request logging, a correlation id. **Never log passwords, tokens or hashes.**
- Rate limiting (`AddRateLimiter`, fixed window) on `register`, `login` and `refresh`.
- Swagger must support Bearer auth (an Authorize button). Add the security scheme through an OpenAPI document transformer. Check the current Microsoft.OpenApi API in .NET 10 first, since it changed from earlier versions.

## C. API contract

| Method | Route | Auth | Body / query | Success |
|---|---|---|---|---|
| POST | `/api/auth/register` | none | `{ email, password, displayName }` | `201` with `{ accessToken, expiresAtUtc, user }` and the refresh cookie |
| POST | `/api/auth/login` | none | `{ email, password }` | `200` same shape as register |
| POST | `/api/auth/refresh` | cookie | none | `200` `{ accessToken, expiresAtUtc, user }`, cookie rotated |
| POST | `/api/auth/logout` | cookie | none | `204`, token family revoked, cookie cleared |
| GET | `/api/auth/me` | Bearer | none | `200` `{ id, email, displayName }` |
| GET | `/api/wallet` | Bearer | none | `200` `{ balance, currency: "INR" }` |
| GET | `/api/wallet/ledger` | Bearer | `page` (default 1), `pageSize` (default 20, max 100) | `200` `{ items, page, pageSize, totalCount }`, newest first |

## D. Database (end state of Phase 1)

- `AspNetUsers` (plus `DisplayName`, `CreatedAtUtc`) and the other standard Identity tables.
- `Wallets(Id, UserId UNIQUE FK, Balance, RowVersion, CreatedAtUtc)`.
- `LedgerEntries(...)` as described above, with the filtered unique index and the paging index.
- `RefreshTokens(Id, UserId FK, TokenHash varbinary(32) UNIQUE, FamilyId INDEXED, CreatedAtUtc, ExpiresAtUtc, RevokedAtUtc NULL, RevokedReason NULL, ReplacedByTokenId NULL, CreatedByIp NULL, RowVersion)`.
- Three migrations: `AddIdentity`, `AddWalletAndLedger`, `AddRefreshTokensAndLedgerGuards`. All EF configuration goes in `IEntityTypeConfiguration<T>` classes.

## E. Angular (what the user sees)

- Dark terminal theme (PaperDesk).
- Standalone components, signals, new control flow (`@if`, `@for`), typed reactive forms, `ChangeDetectionStrategy.OnPush`.
- Dev proxy: `proxy.conf.json` mapping `/api` to `https://localhost:7200`.
- Screens: Login, Register, Dashboard with wallet card and ledger table with server-side paginator.
