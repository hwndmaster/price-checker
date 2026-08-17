---
name: dotnet-tests
description: Conventions and commands for the .NET test suite of the PriceChecker solution — unit, repository, and integration tests (xUnit v3, AutoFixture + FakeItEasy, Genius.Atom TestingUtil, EF Core, WebApplicationFactory + SQLite). Use whenever writing, editing, running, or debugging tests in PriceChecker.Core.Tests, PriceChecker.Db.Tests, PriceChecker.WebApi.Tests, or PriceChecker.WebApi.IntegrationTests, or when measuring .NET code coverage.
---

# PriceChecker .NET tests

The solution (`PriceChecker.slnx`) has four test projects. Root namespace prefix is `Genius.PriceChecker.*`. Central package versions live in `Directory.Packages.props`; shared test wiring is in `Directory.Build.props`.

## Test projects

| Project | Scope |
|---------|-------|
| `PriceChecker.Core.Tests` | Unit tests for the scanning domain: agent handlers (`SimpleRegex`), `PriceSeeker`. |
| `PriceChecker.Db.Tests` | Repository tests against a real EF Core context (`RepositoryTestContext`), plus the legacy JSON importer. |
| `PriceChecker.WebApi.Tests` | Unit tests for WebApi services (e.g. `Services/ScanContextTests`). |
| `PriceChecker.WebApi.IntegrationTests` | End-to-end HTTP tests via `WebApplicationFactory` over in-memory SQLite. |

## Commands

| Task | Command (cwd = repo root) |
|------|---------------------------|
| Run all tests | `dotnet test PriceChecker.slnx` |
| Run one project | `dotnet test PriceChecker.Db.Tests` |
| Filter by name | `dotnet test --filter "FullyQualifiedName~AgentsWorkflowScenario"` |
| Build only | `dotnet build PriceChecker.slnx` |
| Coverage (lcov) | `dotnet test PriceChecker.slnx /p:CollectCoverage=true /p:CoverletOutputFormat=lcov /p:CoverletOutput=./coverage/` |

Coverlet is wired in `Directory.Build.props` for every test project: output format `lcov`, written to each project's `coverage/`. Coverage exclusions (`PriceChecker.Db/Migrations`, all four test assemblies, `[ExcludeFromCodeCoverage]`) are in `CodeCoverage.runsettings`. In VS Code the `test dotnet coverage` task runs the whole solution with coverage; the default test task (`test coverage`) also runs the web coverage afterwards.

## Project wiring

Test projects reference Atom through the MSBuild flags provided by `Genius.Atom.Build.props`, not explicit `PackageReference` entries — set `ReferenceAtomInfrastructure`, `ReferenceAtomData`, `ReferenceAtomDataEf`, `ReferenceAtomWeb` in the `.csproj` as needed, and the matching `*.TestingUtil` packages come along. Only non-Atom packages (e.g. `Microsoft.AspNetCore.Mvc.Testing`, `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.InMemory`) are listed explicitly, always versionless (versions live in `Directory.Packages.props`).

## Frameworks & idioms

- **xUnit v3** — note `TestContext.Current.CancellationToken`; pass it to every async repository/API/file call (existing tests do).
- **AutoFixture + FakeItEasy** — `AutoFixture.AutoFakeItEasy` is globally `using`-imported in test projects (via `Directory.Build.props`). Use FakeItEasy (`A.Fake<T>()`, `A.CallTo(...)`) for fakes; AutoFixture (`new Fixture()`, `_fixture.Build<T>().With(...).Create()`) for data. Add `new OmitOnRecursionBehavior(recursionDepth: 2)` when building graphs that reference back (see `PriceSeekerTests`).
- **Genius.Atom TestingUtil** — `Genius.Atom.Infrastructure.TestingUtil` provides test doubles like `FakeDateTime` (`new FakeDateTime()`, `.Advance(TimeSpan)`) for deterministic time and `FakeLogger<T>` (assert over `.Logs`, e.g. `Assert.DoesNotContain(_logger.Logs, x => x.LogLevel is LogLevel.Error)`). `Genius.Atom.Data.Ef.TestingUtil` supports EF test contexts.
- Assertions use the built-in xUnit `Assert.*` API (`Assert.Equal`, `Assert.Contains`, `Assert.Single`, `Assert.Empty`, `Assert.Null`).
- Test classes are `public sealed`; test methods are `[Fact]` (use `[Theory]` + `[InlineData]` for parameterized cases). Infrastructure helpers inside a test project are `internal sealed`.

## Structure & naming

- Namespace mirrors the project, e.g. `namespace Genius.PriceChecker.Db.Tests.Repositories;`.
- Method name pattern: `MethodOrScenario_GivenSomething_WhenCondition_ThenExpectedOutcome` (e.g. `FullCrudLifecycle_GivenAgent_WhenCreatedUpdatedAndDeleted_ThenAllStagesSucceed`, `NotifyScanStarted_GivenRunningScan_WhenMoreProductsEnqueued_ThenTotalGrows`). Parts are optional — `MethodOrScenario` or `GivenSomething` may be dropped when a scenario does not need them.
- Body uses **Arrange / Act / Assert** comment sections. Lifecycle tests that interleave them use `// Act & Assert: <stage>` per stage.
- Integration scenario tests carry a `/* Scenario Summary + numbered Steps */` block above `[Fact]`, with matching `// Step N:` comments inline — preserve this style when extending them.

## Repository tests (`PriceChecker.Db.Tests`)

- Use `await using var context = new RepositoryTestContext();` for an isolated EF context per test. It is a hand-rolled `IDatabaseContext` over the EF **in-memory** provider with a fresh `Guid` database name, so it bypasses DI entirely — no SQLite, no migrations.
- Inject fakes like `FakeDateTime` into the repository under test: `new AgentsRepository(dateTime, context)`.
- Exercise the full CRUD lifecycle in one test and assert on `GetByIdOrThrowAsync`/`GetAllAsync` results, including deletion leaving the set empty.
- Requests are the `PriceChecker.Dto.RequestMessages` records (`CreateAgentRequest`, `UpdateAgentRequest`, …) — positional, so keep argument order in sync with the record definition.
- Optimistic concurrency: `UpdateAsync` takes the previous `LastModified` token; assert it changes after an update (advance `FakeDateTime` first, otherwise the token may not move).
- `LegacyJsonDataImporterTests` writes real legacy JSON files (`Agent.json`, `Product.json`, `settings.json`) into a temp directory created in the constructor and removed in `Dispose`. Follow that pattern for anything touching the filesystem.
- Changing an entity or `PriceCheckerDbContext` mapping needs a migration: the VS Code `ef migration add` task, or `dotnet ef migrations add <Name> --project PriceChecker.Db --startup-project PriceChecker.Db`. The in-memory repository tests won't catch a missing migration; the integration tests (real SQLite) will.

## Integration tests (`PriceChecker.WebApi.IntegrationTests`)

Infrastructure lives in `Infrastructure/`:
- `PriceCheckerWebApiFactory` — `WebApplicationFactory<Program>` that runs the app under the `IntegrationTests` environment against an in-memory SQLite connection held open for the lifetime of the factory (closing it would drop the database). It also points `Database:LegacyImportPath` at an empty temp directory so the one-time legacy JSON import is skipped, and cleans that directory up on dispose.

Pattern:
```csharp
using var factory = new PriceCheckerWebApiFactory();
using var httpClient = factory.CreateClient();
// ... drive endpoints with PostAsJsonAsync/PutAsJsonAsync/GetStringAsync,
//     parse with JsonDocument.Parse(...).RootElement, assert on JsonElements
```

- Endpoints are versioned: `/api/v1/Agents`, `/api/v1/Products`, `/api/v1/Scans`, `/api/v1/Settings`.
- Create/update responses carry `entityId` and `lastModified`; thread `lastModified` through the next update. A stale token yields `409 Conflict` with `title` = `"Version conflict"`; a duplicate key yields `400 BadRequest` from the FluentValidation validators.
- Request bodies are anonymous objects with camelCase members matching the DTO records.
- There is no shared scenario client here — each test drives `HttpClient` directly. If a third scenario file starts repeating the same request/parse helpers, factor them into `Infrastructure/` rather than copying them again.

## When adding tests

1. Pick the right project (agent handler / seeker logic → Core.Tests; repository or importer → Db.Tests; WebApi service → WebApi.Tests; HTTP/workflow → IntegrationTests).
2. Match the naming, sealed-class, AAA, and (for integration) numbered-step conventions.
3. Run `dotnet test` for that project and confirm green before finishing.
