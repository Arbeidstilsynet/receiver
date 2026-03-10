# 📖 Introduction

Receiver is a repository to receive meldinger / notifications / messages from different sources. The term 'meldinger' is by intention not translated to be consistent with naming conventions within the domain. The main source to receive meldinger from is Digdirs Altinn plattform, which is why this project is using several altinn related terms, also within the domain model.

## 🚀 Start the application

The application exposes a scalar ui, which is locally accesible on [http://localhost:9008/scalar/v1](http://localhost:9008/scalar/v1). Discover the available endpoints on the UI or use the [OpenApi Spec](http://localhost:9008/openapi/v1.json). The OpenApi Spec can be imported by other tools like e.g. Postman.

### Local Development (for developers)

Prerequisites:

- Docker installed
- dotnet SDK installed (v8)
- dotnet-ef tool (dotnet tool install --global dotnet-ef)

To test and debug locally, you need to run the following commands:

```terminal
docker compose -f compose.infra.yaml --profile monitoring up -d
```

> The monitoring profile is optional and can be dropped. If used, a telemetry backend as defined in the [opentelemetry section](#-observability--opentelemetry) is spinned up.

This starts (as per today) a plain postgres instance without any seed, a common valkey service as well as en emulator for the managed google cloud storage service.

Before starting the actual application, we need to create quartz tables for persisting scheduled jobs (this also needs to be done whenever you have changed something withing the entity model, just remember to adjust the migration name):

```terminal
dotnet ef migrations add InitDb --startup-project App/src --project Infrastructure/src -o Db/Migrations
```

Now, start the actual asp dotnet core application:

```terminal
dotnet run --project App/src
```

### Local Development in a complete dockerized environment (for e.g tester)

Prerequisites:

- Docker installed
- dotnet ef migrations have to be up to date / migration files need to be available (see previous section)

Start the application by running:

```terminal
docker compose up --build -d
```

### Cleanup

If you want to stop all containers, run:

```terminal
docker compose down
```

If you want to get a clean database at the next startup, simply remove all the attached volumes:

```terminal
docker compose down -v
```

## 🏃‍♂️ Getting Started

- Spin up altinns app localtest environment together with an altinn app you want to test
- Start this application as described in [#local-development-for-developers](#local-development-for-developers)
- Send a post request to '<http://localhost:9008/subscriptions>' via postman or use scalars UI
- Start the altinn application '<http://local.altinn.cloud/dat/{appId}>' and submit a new form

## 🛠️ Build and Test

In order to ensure that the intended architecture is maintained, there are some basic ArchUnit.Tests ([docs](https://archunitnet.readthedocs.io/en/stable/)) which will ensure that the dependencies do not cross the boundaries established by hexagonal architecture. Enhance the ArchUnit tests with other guidelines you want to maintain, like Namespaces, Naming Conventions and so on.

Also, a couple of sample tests in the different modules are provided to show how things can be tested. In order to run the repository tests, a running docker instance is required. (E.g. [Docker Desktop](https://www.docker.com/products/docker-desktop/))

```terminal
dotnet test
```

## 🧱 Project Structure

<!-- prettier-ignore -->
```md
.
├── App
│   ├── src
│   └── test
├── ArchUnit.Tests
├── Domain
│   ├── Data
│   ├── Logic
│   │   ├── src
│   │   └── test
│   └── Ports
│       ├── App
│       └── Infrastructure
└── Infrastructure
    ├── src
    └── test
```

- ArchUnit.Tests
  - Important tests to gurantee the below structure keeps maintained
- Domain
  - **Domain.Logic** _implements_ Domain.Ports.App and _uses_ Domain.Ports.Infrastructure
  - **Domain.Logic.Test** contains tests that validate the domain logic
  - **Domain.Data** contains Classes/DTOs which can be shared across layers
- Infrastructure (Outgoing: infrastructure the application talks with)
  - **Infrastructure** _implements_ Domain.Ports.Infrastructure (i.e. Adapters)
  - **Infrastructure.Test** contains tests that validate the Infrastructure
- App (Incoming: adapters to make it possible to talk with the application)
  - **App** _uses_ Domain.Ports.App
  - Responsible for injecting the necessary dependencies and exposing API endpoints
  - **App.Test** contains typically integration tests

> Domain.Logic and Infrastructure implementations are internal, and only exposed through DependencyInjection extensions.

## 👩‍💻 Logging

We want to use structured Logging in order to read, filter and query logs in an easy manner. See [Logging in C# and .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging?tabs=command-line).
That means, if you are not in the API Adapter Layer (or not where you start the application), you can simply inject a logger Instance via the constructor of a class (like `ILogger<ExampleClass> logger`). Feel free to write logging extension methods in order to streamline your logging even more.

In all places where you can`t use DependencyInjection, a logger instance can be created the following way:

```csharp
ILogger<Function> logger = LoggerFactory.Create(builder => builder.AddConsole().AddJsonConsole()).CreateLogger<Function>();
```

To ensure that our logging stays consistent, there are ArchUnit tests which check if other logging mechanisms are used and which will fail in that case.

## 🔎 Observability / OpenTelemetry

[General information about observability](https://opentelemetry.io/docs/what-is-opentelemetry/)

We strongly recommend to use the default opentelemetry setup in this application. It ensures a minimal level of observability and can be fine-tuned if needed. When running `docker-compose` with the monitoring profile, a default backend of a `opentelemetry-collector`, `mimir/prometheus` (Metrics), `loki` (Logs), `tempo` (Traces) and `grafana` (Dashboard) is started. You will find the same or a similar setup in our production environment. To explore the telemetry data your application is producing, check out [Grafana Explore](http://localhost:4000/explore).

### Traces

Checkout [OpenTelemetry .NET Traces](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/trace/README.md) for best practices, fine-tuning and examples.

#### Configuration example

```csharp
services.AddOpenTelemetry()
        .WithTracing(options =>
            {
                options.AddAspNetCoreInstrumentation();
                options.AddHttpClientInstrumentation();
                options.AddEntityFrameworkCoreInstrumentation();
                options.AddNpgsql();
                options.AddOtlpExporter();
            })
```

### Logs

Checkout [OpenTelemetry .NET Logs](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/logs/README.md) for best practices, fine-tuning and examples.

#### Configuration example

```csharp
services.AddOpenTelemetry()
        .WithLogging(
            logging => logging.AddOtlpExporter(),
            options => options.IncludeFormattedMessage = true
        );
```

### Metrics

Checkout [OpenTelemetry .NET Metrics](https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/docs/metrics/README.md) for best practices, fine-tuning and examples.

#### Configuration example

```csharp
services.AddOpenTelemetry()
        .WithMetrics(options =>
            {
                options.AddAspNetCoreInstrumentation();
                options.AddHttpClientInstrumentation();
                options.AddOtlpExporter();
            });
```

## 📝 Further reads

[Medium: Hexagonal Architecture, there are always two sides to every story](https://medium.com/ssense-tech/hexagonal-architecture-there-are-always-two-sides-to-every-story-bc0780ed7d9c)
