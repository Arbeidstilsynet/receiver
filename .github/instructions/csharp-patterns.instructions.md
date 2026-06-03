---
description: "Use when writing or modifying C# in the receiver service: adding a feature slice (port → domain service → infrastructure adapter → controller), DI registration and Configuration records, Mapster mapping, FluentValidation, EF Core entities/migrations, observability (Tracer/ApiMeters/logging), or ArchUnit and fixture-based tests."
name: "Receiver C# patterns"
applyTo: "src/**/*.cs"
---

# Receiver C# patterns

This is a hexagonal (ports & adapters) .NET service. Namespace prefix:
`Arbeidstilsynet.MeldingerReceiver`. Implementations are `internal` and exposed only via DI
extensions. **ArchUnit.Tests enforce these rules — run `dotnet test` (from `src/`) after changes.**

Layer rules:

- `Domain.Logic` and `Infrastructure` types are `internal`, live under their assembly's namespace, and must not use `System.Console` (use `ILogger<T>`).
- Domain must not depend on infrastructure SDKs directly — only on `Domain.Ports.Infrastructure` interfaces.
- `Domain.Ports.App` = driving ports (App → Domain). `Domain.Ports.Infrastructure` = driven ports (Domain → outside).

## Adding a feature end-to-end

1. **Define/extend a driving port** in `Domain/Ports/App` — a `public interface IFooService` with `CancellationToken` params. Request/response DTOs live alongside it or in `Domain.Data`.
2. **Implement it** as `internal class FooService : IFooService` in `Domain/Logic/src`. Inject ports + `ILogger<FooService>` + `IMapper` via constructor.
3. **Add any driven ports** you need (e.g. `IFooRepository`) in `Domain/Ports/Infrastructure`, and implement them as `internal` adapters in `Infrastructure/src`.
4. **Register** in the relevant `DependencyInjection` class (see below).
5. **Expose over HTTP** with a thin controller in `App/src/WebApi/Controllers` that maps to the domain request, calls the service, and returns a response model.
6. **Test**: domain unit tests in `Domain/Logic/test`, infra tests in `Infrastructure/test`, integration tests in `App/test`.

## DI registration + Configuration records

Each module has a `public static class DependencyInjection` with an `AddX` extension. Concrete
types are registered against their ports; configuration is a `record` with `required` `init`-only
members, wrapped via `Options.Create(...)`.

```csharp
public record FooConfiguration
{
    public required string BaseUrl { get; init; }
}

public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    InfrastructureConfiguration configuration)
{
    services.AddScoped<IFooRepository, FooRepository>();
    services.AddSingleton(Options.Create(configuration));
    services.AddMapper();
    return services;
}
```

Conditional adapters (real vs. dummy) are chosen from config — see the virus-scan / notification
branches in `Infrastructure/src/DependencyInjection.cs`. Register multiple implementations of the
same port (e.g. `IPostMeldingPersistedAction`) when you want them all to run.

## Mapster mapping

Mapping uses Mapster. `AddMapper()` scans the assembly for `IRegister` classes; config sets
`RequireDestinationMemberSource = true`, so every destination member must have a source.

```csharp
internal class Mapper : IRegister
{
    public void Register(TypeAdapterConfig config) =>
        config.NewConfig<SourceDto, DestDto>()
              .NameMatchingStrategy(NameMatchingStrategy.Flexible);
}
```

Use `IMapper.Map<T>(...)` in services. Simple per-type transforms can also live in
`Extensions/*MappingExtensions.cs`.

## Controllers + FluentValidation

Controllers are thin: validate, map to a domain request, call the service, return a response model.

```csharp
[ApiController]
[Route("[controller]")]
public class FooController(IFooService service, IValidator<FooBody> validator) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<FooResponse>> Post([FromForm] FooBody body, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(body, ct);
        if (!validation.IsValid) return BadRequest(validation.ToString());
        var result = await service.Process(body.ToDomainRequest(), ct);
        return new FooResponse { Id = result.Id };
    }
}
```

Validators are `internal class XValidator : AbstractValidator<X>` in `App/src/WebApi/Validation`,
registered with FluentValidation. Map exceptions to status codes via `AddExceptionMapping<T>` in
`StartupExtensions` rather than catching in controllers.

## EF Core entities + migrations

- `ReceiverDbContext` is in `Infrastructure/src/Db`; entities in `Db/Model` derive from `BaseEntity`.
- Migrations history table is `ef_migrations_history`. After changing the model, add a migration
  (from `src/`):
  ```sh
  dotnet ef migrations add <DescriptiveName> --startup-project App/src --project Infrastructure/src -o Db/Migrations
  ```
- Repositories are `internal` adapters implementing `Domain.Ports.Infrastructure` interfaces.

## Observability

- Each assembly has an `internal static Tracer` exposing an `ActivitySource` named after the layer.
  Wrap notable operations: `using var activity = Tracer.Source.StartActivity();`
- Metrics flow through `ApiMeters`; register custom meters in `StartupExtensions`.
- Logging: inject `ILogger<T>`; add reusable helpers to `Extensions/LoggerExtensions.cs` with named
  placeholders and `{@destructured}` objects. Never use `System.Console` or string interpolation in
  log templates.

```csharp
logger.LogInformation("Received request to process melding for {MeldingId}", meldingId);
```

## Tests

- xUnit on `Microsoft.Testing.Platform`. Integration tests require Docker running.
- DI-backed tests use `TestBedFixture` (`Xunit.Microsoft.DependencyInjection`); see
  `Domain/Logic/test/fixtures`. Infrastructure/App tests use dedicated read-only vs. write
  fixtures that spin up Postgres/Storage (`*ReadOnlyTestFixtureWithDb`, `*WriteTestFixtureWithDb`).
- Generate test data with Bogus/Faker helpers (`FakerExtensions`).
- When you touch layering, visibility, namespaces, or logging, the **ArchUnit.Tests** are the
  guardrail — keep them green.
