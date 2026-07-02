# Receiver — Agent Guide

`receiver` is a .NET service that receives *meldinger* (messages/documents) from external
sources — primarily Digdir's Altinn platform — scans and stores them, persists structured
data, and notifies registered consumers over Valkey streams. Domain terms are kept in
Norwegian on purpose (e.g. `Melding`, `Subscription`, `ConsumerManifest`).

> The detailed coding patterns for this repo live in
> [`.github/instructions/csharp-patterns.instructions.md`](instructions/csharp-patterns.instructions.md)
> and are auto-applied when editing receiver C# files.

## Documentation map

- [README](../src/README.md) — getting started, project structure, logging, observability
- [documentation/architecture.md](../documentation/architecture.md) — how the publisher/consumer flow works
- [documentation/configuration.md](../documentation/configuration.md) — environment variables and deployment

## Build, run and test

Run .NET commands (`dotnet`, `csharpier`, `dotnet ef`) from the `src/` directory
(solution: `MeldingerReceiver.slnx`). Run `pnpm` and `docker` commands from the repository root.

```sh
# Start infra (postgres, valkey, GCS emulator); add --profile monitoring for telemetry
docker compose -f compose.infra.yaml up -d

# Run the API (Scalar UI at http://localhost:9008/scalar/v1)
dotnet run --project App/src

# Run all tests (Docker MUST be running — integration tests use Testcontainers-style fixtures)
dotnet test

# Format / check formatting (CSharpier, pinned in .config/dotnet-tools.json)
dotnet tool restore
dotnet csharpier format .
dotnet csharpier check .
```

- **SDK: .NET 10** (`global.json` is the source of truth). Test runner is `Microsoft.Testing.Platform`.
- EF Core migrations (run from `src/`):
  ```sh
  dotnet ef migrations add <Name> --startup-project App/src --project Infrastructure/src -o Db/Migrations
  ```

## Architecture — hexagonal (ports & adapters)

Three modules, with dependencies pointing inward toward the domain:

- **Domain** — the core.
  - `Domain.Ports.App` — *driving* ports (interfaces the App calls into).
  - `Domain.Ports.Infrastructure` — *driven* ports (interfaces the domain needs from the outside).
  - `Domain.Logic` — implements `Ports.App`, depends on `Ports.Infrastructure`. **Internal** classes.
  - `Domain.Data` — DTOs/models shared across layers.
- **Infrastructure** (outgoing adapters) — implements `Ports.Infrastructure` (Postgres/EF Core, Google Cloud Storage, Valkey, ClamAV virus scan, Altinn). **Internal** classes.
- **App** (incoming adapters) — ASP.NET Core host, controllers, validation, jobs, composition root. Uses `Ports.App`.

Implementations are `internal` and exposed **only** through `AddDomain` / `AddInfrastructure`
DI extension methods. These boundaries are enforced by **ArchUnit.Tests** — if you cross a
layer boundary, change visibility, or use `System.Console` instead of `ILogger<T>`, those
tests fail. Run them before assuming a change is correct.

## Conventions (high-level — see the patterns instructions file for details and examples)

- Each module has a public `DependencyInjection` class with an `AddX` extension and a
  `XConfiguration` record (`required`, `init`-only). Register concrete types against ports.
- Mapping uses **Mapster** (`IRegister` classes + `MappingExtensions`).
- Request validation uses **FluentValidation** (`AbstractValidator<T>`), invoked in controllers.
- Always thread `CancellationToken` through async call chains.
- Structured logging via injected `ILogger<T>`; prefer `LoggerExtensions` helpers with named
  placeholders (`{MeldingId}`), never string interpolation. No `Console` (ArchUnit-enforced).
- Observability: each assembly has an `internal static Tracer` `ActivitySource`; wrap notable
  work in `using var activity = Tracer.Source.StartActivity();`. Metrics go through `ApiMeters`.
- Run `dotnet csharpier format .` before committing.

## Change checklist (API/release-sensitive work)

When a change touches API surface, generated client types, or versioning/release metadata, complete
this checklist before pushing:

1. Regenerate OpenAPI + TS types:
   ```sh
   pnpm generate:types
   ```
2. Run relevant tests/builds:
   ```sh
   dotnet build
   dotnet test
   ```
3. If version is bumped (`src/Publish/Receiver.Publish/Receiver.Publish.csproj`), update `src/CHANGELOG.md`
   in the same change.
4. Verify generated artifacts are committed when changed (`src/generated/openApi.json`,
   `src/generated/types.d.ts`).
