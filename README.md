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
| `PriceChecker.WebApi` | ASP.NET Core API: CRUD controllers, scan orchestration, SignalR scan hub, scheduled scan background service. |
| `PriceChecker.Web` | React + TypeScript frontend (Vite, PrimeReact, redux-saga), talking to the API via an NSwag-generated client and live scan updates over SignalR. |
| `PriceChecker.AppHost` | .NET Aspire orchestration for local development and the telemetry dashboard in Docker. |

Built on top of the [Atom framework](https://github.com/hwndmaster/atom). The old WPF frontend is preserved on the `archive-wpf-frontend` branch.

## Development

Prerequisites: .NET 10 SDK, Node.js 22+, pnpm (via `corepack enable`), and a GitHub Packages token
(`read:packages`) configured for NuGet and in `~/.npmrc` (`//npm.pkg.github.com/:_authToken=...`).

Open `price-checker.code-workspace` in VS Code to get the grouped one-click commands
(`PriceChecker.Web`, `PriceChecker (.NET)`, `PriceChecker (Docker)`) in the Commands view —
they require the [`usernamehw.commands`](https://marketplace.visualstudio.com/items?itemName=usernamehw.commands)
extension. `F5` offers the launch configurations, and `Ctrl+Shift+B` / `Ctrl+Shift+P → Run Test Task`
run the build and coverage tasks.

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

## Agents

An agent is one recipe for reading a price off one site. It carries a URL template with a `{0}`
placeholder for the source's argument, a price pattern whose `price` group captures the amount,
the decimal delimiter that amount uses, and the handler that applies them.

It may also carry a **URL pattern**: a regular expression matching the product URLs that site serves,
capturing the argument in a group named `arg`. That is what lets a source be added by pasting a
product URL into the product form — the API answers which agents can scan that URL and what their
argument would be, and the first, most specific match is filled in. An agent without a URL pattern
still takes part: its URL template is matched in reverse, which covers the sites whose product URL
is exactly what the template builds, but not the ones that serve a product under several URLs
(Amazon puts an arbitrary product slug in front of the ASIN). An explicit URL pattern always wins
over a reverse-matched template.

Agents are data, not code. `Data/Agent.json` seeds a fresh install (see [Data](#data) below), and
from then on they are edited in the UI — a site that moves its price markup is repaired by fixing
its agent, not by releasing the scanning code.

## Automatic price scans

All products are re-scanned once a day, configured under `Scanning:Schedule` in the API's
`appsettings.json` — there is no in-app setting for it:

| Key | Default | Meaning |
| --- | --- | --- |
| `Enabled` | `true` | Whether the daily scan runs. Manual scans from the UI are unaffected. |
| `TimeOfDay` | `09:00` | Wall-clock time of the scan, in `TimeZone`. |
| `TimeZone` | `Europe/Amsterdam` | Explicit rather than machine-local, since containers run on UTC. |
| `OutdatedGrace` | `03:00:00` | How long after the scan a product may stay unscanned before the list marks it "Outdated". |

Whether a run is due is derived from the products' own last scan dates, not from a timer, so an
overnight shutdown or a restart does not skip a day — the scan happens as soon as the API is up
again, and never twice for the same day.

### Pacing

A scan does not fetch everything at once. All the sources of a scan — a daily one or a manual one
from the UI — are regrouped into one queue per domain, resolved from the agent URL, so that a site
shared by several agents and several products is reached by one request at a time, while the other
sites are scanned in parallel. A product is reported as scanned once its last source has been, whichever queue that was.

Configured under `Scanning:Pacing`:

| Key | Default | Meaning |
| --- | --- | --- |
| `SameDomainDelay` | `00:00:30` | Pause between two requests to the same domain. The first request to a domain is never delayed; zero leaves only the per-domain serialization. |
| `SameDomainDelayJitter` | `0.2` | How far a pause may deviate from `SameDomainDelay`, as a fraction of it: `0.2` spreads a 30 second pause over 24–36 seconds, so the requests do not arrive on an exact, machine-like beat. |
| `MaxParallelDomains` | `8` | How many domains are scanned in parallel, and with that the upper bound of concurrent outgoing requests. |

A scan therefore takes about `SameDomainDelay` × (sources of the busiest domain − 1) at the least.

## Telegram notifications

A daily scan that finds a better price reports it to a Telegram chat — one message per scan,
listing everything it found, rather than one per product. Only the automatic daily scan reports:
a scan started from the UI is one you are watching, where the result is already in front of you.

What counts as "better" is per product:

- A product **without** a target price is reported when it beats its own lowest price ever.
- A product **with** a target price is reported when the price currently available reaches that
  target, and only on the crossing — a product parked below its target is not reported again by
  every following scan. The comparison is against the current price rather than the all-time
  lowest, which may date from months ago and no longer be available.

The target price is set per product in the product form, behind the "Notify at a target price"
checkbox. Clearing it is what switches the product back to lowest-price notifications.

Configured under `Telegram` in the API's `appsettings.json`, and left empty the integration is
simply off — nothing is sent and nothing fails:

| Key | Meaning |
| --- | --- |
| `BotToken` | The Telegram Bot API token. |
| `ChatId` | The chat the notifications are sent to. |

In Docker they come from the environment instead, so the token stays out of the repository —
set `TELEGRAM_BOT_TOKEN` and `TELEGRAM_CHAT_ID` in `.env` next to `ATOM_PKG_ACCESS_TOKEN`. The
startup summary line reports whether the integration is wired up, never the token itself.

## Data

The SQLite database lives in `Data/PriceChecker.db` next to the API binaries and is migrated
automatically on startup, with periodic backups (see the `Database:Backup` settings).

On the first run against an empty database, the legacy JSON data files (`Agent.json`,
`Product.json`) are imported automatically. Locally they are picked up from
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
