# Price Checker

A simple price checker for the selected products to catch the cheapest deal.

_CI Status:_<br/>
<img src="https://github.com/hwndmaster/price-checker/actions/workflows/ci.yml/badge.svg?branch=master"><br>

## Architecture

| Project | Purpose |
| --- | --- |
| `PriceChecker.Core` | The scanning domain: agent handlers, the price seeker, scan status logic. |
| `PriceChecker.Dto` | Wire contracts: strongly typed references, DTOs, request messages. |
| `PriceChecker.Db` | SQLite persistence: EF Core entities, migrations, repositories, the one-time legacy JSON import. |
| `PriceChecker.WebApi` | ASP.NET Core API: CRUD controllers, scan orchestration, SignalR scan hub, auto-refresh background service. |
| `PriceChecker.Web` | React + TypeScript frontend (Vite, PrimeReact, redux-saga), talking to the API via an NSwag-generated client and live scan updates over SignalR. |
| `PriceChecker.AppHost` | .NET Aspire orchestration for local development and the telemetry dashboard in Docker. |

Built on top of the [Atom framework](https://github.com/hwndmaster/atom). The old WPF frontend is preserved on the `archive-wpf-frontend` branch.

## Development

Prerequisites: .NET 10 SDK, Node.js 22+, pnpm (via `corepack enable`), and a GitHub Packages token
(`read:packages`) configured for NuGet and in `~/.npmrc` (`//npm.pkg.github.com/:_authToken=...`).

The single F5 target is the Aspire AppHost, which starts the API, the Vite dev server, and the dashboard:

```bash
dotnet run --project PriceChecker.AppHost
```

Or run the parts individually:

```bash
dotnet run --project PriceChecker.WebApi     # API on http://localhost:5080
pnpm --dir PriceChecker.Web start            # Frontend on http://localhost:5081
```

Tests:

```bash
dotnet test PriceChecker.slnx
pnpm --dir PriceChecker.Web test -- --run
```

After changing the API surface, regenerate the TypeScript client (with the API running):

```bash
pnpm --dir PriceChecker.Web nswag
```

## Data

The SQLite database lives in `Data/PriceChecker.db` next to the API binaries and is migrated
automatically on startup, with periodic backups (see the `Database:Backup` settings).

On the first run against an empty database, the legacy JSON data files (`Agent.json`,
`Product.json`, `settings.json`) are imported automatically. Locally they are picked up from
the repository `Data/` folder; in Docker, copy them into the mounted `Data-Local/Data/` folder
before the first start.

## Docker

Set `ATOM_PKG_ACCESS_TOKEN` in `.env` (never commit a real token), then:

```bash
docker compose up --build -d
```

| Service | Address |
| --- | --- |
| Web UI | http://localhost:5081 |
| API | http://localhost:5080 |
| Aspire dashboard (telemetry) | http://localhost:15180 |

Publishing the images to a registry: `./publish-docker.ps1` (see the parameters inside).
