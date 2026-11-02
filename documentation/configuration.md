# Configuration

| (env) Variable Name                                              | Required | Example value                                                                         |
|------------------------------------------------------------------|----------|---------------------------------------------------------------------------------------|
| ASPNETCORE_ENVIRONMENT                                           | yes      | Development                                                                           |
| ASPNETCORE_HTTP_PORT                                             | no       | "8080"                                                                                |
| API__Cors__AllowedOrigins__0                                     | no       | <http://localhost:3000>                                                               |
| API__Cors__AllowCredentials                                      | no       | false                                                                                 |
| Domain__AllowOnlyRegisteredApps                                  | no       | true                                                                                  |
| Domain__RequireAltinnDeletionOnUnsubscribe                       | no       | true                                                                                  |
| Infrastructure__MaskinportenConfiguration__IntegrationId         | yes*     |                                                                                       |
| Infrastructure__MaskinportenConfiguration__CertificatePrivateKey | yes*     |                                                                                       |
| Infrastructure__MaskinportenConfiguration__CertificateChain      | yes*     |                                                                                       |
| Infrastructure__AltinnConfiguration__Environment                 | no**     | Local                                                                                 |
| Infrastructure__AltinnConfiguration__LocalAuthenticationLevel     | no       | 4                                                                                     |
| Infrastructure__AltinnClientsConfiguration__Storage__Scopes__0   | no       | altinn:serviceowner/instances.read                                                     |
| Infrastructure__AltinnClientsConfiguration__Events__Scopes__0    | no       | altinn:events.subscribe                                                               |
| Infrastructure__AltinnClientsConfiguration__Events__Scopes__1    | no       | altinn:serviceowner/instances.read                                                     |
| Infrastructure__AltinnClientsConfiguration__Apps__Scopes__0      | no       | altinn:serviceowner/instances.write                                                    |
| Infrastructure__PostgresConfiguration__ConnectionString          | yes      | Host=postgres-receiver;Port=5432;Database=demo_db;Username=postgres;Password=postgres |
| Infrastructure__DocumentStorageConfiguration__BaseUrl            | no       | <http://file-storage:4443/storage/v1/>                                                |
| Infrastructure__DocumentStorageConfiguration__AuthRequired       | no       | true                                                                                  |
| Infrastructure__DocumentStorageConfiguration__BucketName         | yes      | test-bucket                                                                           |
| Infrastructure__ValkeyConfiguration__ConnectionString            | yes      | valkey:6379                                                                           |
| Infrastructure__VirusScanConfiguration__BaseUrl                  | yes      | <http://clamav-web:8080/>                                                             |
| Infrastructure__AppDomain                                        | yes      | <http://receiver-api:8080>                                                            |
| Infrastructure__SkipVirusScan                                    | no       | false                                                                                 |
| OTEL_EXPORTER_OTLP_ENDPOINT                                      | no       | http://monitoring_otel:4317                                                           |

\* required for communication with altinns staging and prod systems

\** Production always targets Altinn Production. Staging defaults to Tt02. Other host
environments must explicitly select an Altinn target (`Local`, `Tt02`, or `Production`).

## Optional configuration for local Altinn tests

Receiver binds `Infrastructure:AltinnConfiguration` directly to the Common.Altinn
configuration, including the optional `LocalAuthenticationLevel` setting.
Leave it unset for normal use: local organization test tokens default to authentication
level **4**. To exercise lower-authentication scenarios, override it for that test run.
Valid values are **1 through 4**; invalid values fail during startup when targeting Local.
The setting is ignored when targeting Altinn TT02 or Production and does not change
receiver's own API authentication.

For example, from `src/` in PowerShell:

```powershell
$env:Infrastructure__AltinnConfiguration__Environment = "Local"
$env:Infrastructure__AltinnConfiguration__LocalAuthenticationLevel = "2"
dotnet run --project App\src
```

Remove the override after the test to restore the default:

```powershell
Remove-Item Env:\Infrastructure__AltinnConfiguration__LocalAuthenticationLevel
```

For automated tests, prefer per-test host configuration overrides (for example,
`ConfigureAppConfiguration` with `AddInMemoryCollection`) using
`Infrastructure:AltinnConfiguration:Environment` and
`Infrastructure:AltinnConfiguration:LocalAuthenticationLevel`. Avoid changing process-wide
environment variables in parallel tests. The level applies to all Altinn clients in that
receiver host, so use a separate host for each authentication-level scenario.
No separate test-only section or HTTP endpoint is needed, and shared appsettings files
should leave the level unset.

## Run with docker compose

Use the compose files `compose.yaml` and `compose.infra.yaml` provided in the repository root.
The `compose.yaml` contains the receiver app and the described configuration above.

```terminal
docker compose -f compose.infra.yaml up -d
```

> For the docker setup, we use the receiver image pushed to ghcr, which also includes prerelease versions. All available versions can be found [here](https://github.com/Arbeidstilsynet/receiver/pkgs/container/receiver).

## Run in nais-cluster

Use (and adjust) the provided examples in [./examples/.nais](./examples/.nais).

> For the nais setup, we publish the same receiver image to our nais repository. We do not publish prerelease versions in nais. The latest version will be in accordance with our published [releases](https://github.com/Arbeidstilsynet/receiver/releases).
