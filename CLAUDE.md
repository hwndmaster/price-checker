# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

**PriceChecker** — tracks product prices by scanning configured sources and reporting when one drops.
A .NET solution on the **Genius.Atom** framework plus a React SPA on the `@hwndmaster/atom-*`
packages. Repository: <https://github.com/hwndmaster/price-checker>.

Root namespace prefix is `Genius.PriceChecker.*`. Compose service prefix is **`pricechecker`**
(no hyphen) — note it differs from the repo directory name, which is what Docker Compose uses as the
project name.

## Stack conventions live in plugins

The conventions for this stack are **not** in this repo. They come from the
[`geni-ai-sdlc-marketplace`](https://github.com/hwndmaster/geni-ai-sdlc-marketplace) plugins:

| Plugin | Covers |
|--------|--------|
| `atom-backend` | .NET test conventions — xUnit v3, AutoFixture + FakeItEasy, Atom `TestingUtil`, repository and schema tests, integration tests, the Atom build coupling |
| `atom-frontend` | SPA conventions — store slice layout, sagas, PrimeReact, Vitest, ESLint import boundaries |
| `atom-devops` | `publish-docker.ps1`, `packages-cleanup.ps1`, `ci.yml`, `nuget.config`, `CodeCoverage.runsettings` and friends — edit the plugin template, never the copy here |

This file carries only what is specific to *this* app.

## Solution map

`PriceChecker.slnx`

| Project | Scope |
|---------|-------|
| `PriceChecker.AppHost` | Aspire host — dashboard and orchestration |
| `PriceChecker.Core` | The scanning domain — agent handlers, `PriceSeeker` |
| `PriceChecker.Db` | EF Core context, entities, migrations, repositories, the legacy JSON importer |
| `PriceChecker.Dto` | The contracts every other project shares: DTOs, references, request messages and the enums that travel with them. It references nothing of ours — `Core` references **it**, not the other way round, so that a type Core states on its own boundary can be a `ProductRef` rather than a `Guid`. |
| `PriceChecker.WebApi` | ASP.NET Core API |
| `PriceChecker.Web` | React SPA |
| `PriceChecker.Core.Tests` | Scanning domain: agent handlers (`SimpleRegex`), `PriceSeeker` |
| `PriceChecker.Db.Tests` | Repository tests plus the legacy JSON importer |
| `PriceChecker.WebApi.Tests` | WebApi services (e.g. `Services/ScanContextTests`) |
| `PriceChecker.WebApi.IntegrationTests` | End-to-end HTTP tests |

This is the only app in the set with a `Core.Tests` project.

Entity ids are **`Guid`** — `models/types.ts` uses `EntityGuidId` with `createGuidRefConverter`,
exposing `agentRef`, `productRef`, `productSourceRef` and `productPriceRef`. Use the factories
(`productRef(guid)`, `productRef.default()` for a new entity) in app code and in tests, never raw
strings.

## Ports

| What | Port |
|------|------|
| WebApi (local) | 5080 |
| Web dev server | 5081 |

## Agents

An agent carries an optional **`UrlPattern`** besides its `Url` template: a regex matching the
product URLs of its site, capturing the argument in a group named `arg`. `ISourceUrlRecognizer`
(Core) matches a pasted URL against it, falling back to a reverse-matched `Url` template for the
agents that have none, and `POST api/v1/Agents/recognize` exposes it to the product form's
"Add from URL". Explicit patterns rank before reverse-matched templates, then by how much of the
URL was matched besides the argument.

The shipped agents are data, not code: `Data/Agent.json` seeds a fresh install through the legacy
importer, and from then on they are edited in the UI. A site that moves its price markup is repaired
by editing its agent; keep `Data/Agent.json` in step so a fresh install starts out repaired too, and
`Data/Product.json` alongside it, since its sources reference agents by key.

## Backend specifics

- **Repository tests** use the local `RepositoryTestContext` (EF in-memory, fresh `Guid` database
  name) rather than Atom's `BaseRepositoryTests`.
- **`LegacyJsonDataImporterTests`** writes real legacy JSON files (`Agent.json`, `Product.json`,
  `settings.json`) into a temp directory created in the constructor and removed in `Dispose`. Follow
  that pattern for anything touching the filesystem.
- **AutoFixture**: `PriceSeekerTests` needs `new OmitOnRecursionBehavior(recursionDepth: 2)` for the
  object graphs that reference back into themselves.
- **Integration tests** have only `PriceCheckerWebApiFactory` in `Infrastructure/` — there is **no
  `ApiScenarioClient` here**; each test drives `HttpClient` directly. The factory points
  `Database:LegacyImportPath` at an empty temp directory so the one-time legacy import is skipped, and
  cleans it up on dispose. If a third scenario file starts repeating the same request/parse helpers,
  factor them into `Infrastructure/` rather than copying them a third time.

## Frontend specifics

`PriceChecker.Web`, dev server on 5081. `pnpm nswag` needs the API running on 5080.

- **Every query is a saga**, the ones that keep nothing in the redux store included. Such a saga
  resolves the caller's callback with the type the caller wants: `fetchAgentHandlers` with `string[]`,
  `recognizeSourceUrl` with `RecognizedSource[]`.
- **`messages.ts` holds the representations of the API types that are not models**, so that the rest
  of the app works in its own types. Today that is the payloads the scan hub pushes.
- `productEdit` refetches the agents on every mount rather than only when the list is empty, because
  an agent recognized from a pasted URL has to be in it.
- **Store slices**: `agents`, `products`, `scans`. There is no `settings` slice — it was dropped in
  `persistVersion: 2`, along with `products.editedProduct`.
- `persistBlacklist: ["common", "scans"]` — scan progress is transient.
- **SignalR**: `scans/messages.ts` owns the hub connection (`startScanHubConnection`,
  `${ApiUrl}/hubs/scan`) and dispatches store actions from the hub callbacks. This is the only app in
  the set with live push, and the only slice with a `messages.ts`.
- `fakeAxios` lives at `@/store/testUtils/sagas`; shared model factories are in
  `utils/tests/testModels.ts` (`createAgent`, `createProduct`, `createProductOverview`,
  `createProductPrice`, `createScanProgress`).
- Loading targets in play: `Agents`, `AgentEdit`, `Products`, `ProductEdit`, `ProductPrices` — the
  edit-scoped targets are how a form blocks independently of its list.
