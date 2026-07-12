# ExpenseTracker

A .NET 10 backend for tracking personal income and expenses, plus shared expense lists that split
costs fairly across a group and settle up with a minimal set of transfers.

## Features

- **Auth** — register, login, refresh, logout. JWT access tokens with rotating, hashed refresh
  tokens; account lockout and rate limiting on the auth endpoints.
- **Personal ledger** — income and expense transactions with user-owned categories, filtering and
  paging.
- **Shared expense lists** — role-based membership (Viewer / Editor / Owner), per-list categories,
  transactions with equal or custom splits, balances, and greedy debt simplification.
- **Mock members** — placeholders for people without an account. Editors record expenses and
  settlements on their behalf; when the person joins, they claim the placeholder and inherit its
  history.
- **Closing a list** — freezes it and files each member's share of the expenses into their personal
  ledger under a category named after the list. Reopening withdraws those rows again. Toggleable
  per user in settings.
- **Dashboard** — income/expense/net for a period against the preceding one, and a spending
  breakdown by category.

## Architecture

Clean Architecture, four projects:

| Project | Role |
|---|---|
| `ExpenseTracker.Domain` | Entities, enums, split arithmetic. No dependencies. |
| `ExpenseTracker.Application` | CQRS handlers (MediatR), DTOs, FluentValidation validators, interfaces. |
| `ExpenseTracker.Infrastructure` | EF Core + SQL Server, ASP.NET Core Identity, JWT, balance calculation. |
| `ExpenseTracker.API` | Controllers, middleware, DI, Swagger. |

Notable conventions:

- Every command and query goes through MediatR, with a validation behavior in front of the handler.
- All entities are soft-deleted: a `SaveChanges` interceptor rewrites `Deleted` into
  `IsDeleted = true`, and a global query filter hides those rows.
- Authorization lives in the handlers, not the controllers. Non-members get `404` rather than `403`,
  so list ids cannot be probed.

## Getting started

Requires the .NET 10 SDK and SQL Server (LocalDB is fine).

Configuration is **not** committed. `Jwt:Key` and the connection string must be supplied via
user-secrets locally, or environment variables when deploying. The app fails at startup with an
explicit message if either is missing.

```bash
cd src/ExpenseTracker.API

# a 256-bit key is the minimum for HMAC-SHA256
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Server=(localdb)\\mssqllocaldb;Database=ExpenseTracker;Trusted_Connection=True;MultipleActiveResultSets=true"
```

Apply the migrations and run:

```bash
dotnet ef database update --project src/ExpenseTracker.Infrastructure --startup-project src/ExpenseTracker.API
dotnet run --project src/ExpenseTracker.API
```

Swagger is served at `/swagger` in Development. `/health` reports database connectivity.

When deploying, the same settings are read from `Jwt__Key` and
`ConnectionStrings__DefaultConnection`.

## Tests

```bash
dotnet test ExpenseTracker.slnx
```

The suite runs against SQLite in-memory rather than the EF InMemory provider, because the handlers
open real transactions and depend on query filters. It concentrates on the money: that shares always
sum to the transaction amount, that balances always sum to zero, that income moves the opposite way
to an expense, and that closing a list projects each member's share exactly once.
