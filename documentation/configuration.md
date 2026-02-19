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
| Infrastructure__PostgresConfiguration__ConnectionString          | yes      | Host=postgres-receiver;Port=5432;Database=demo_db;Username=postgres;Password=postgres |
| Infrastructure__DocumentStorageConfiguration__BaseUrl            | no       | <http://file-storage:4443/storage/v1/>                                                |
| Infrastructure__DocumentStorageConfiguration__AuthRequired       | no       | true                                                                                  |
| Infrastructure__DocumentStorageConfiguration__BucketName         | yes      | test-bucket                                                                           |
| Infrastructure__ValkeyConfiguration__ConnectionString            | yes      | valkey:6379                                                                           |
| Infrastructure__VirusScanConfiguration__BaseUrl                  | yes      | <http://clamav-web:8080/>                                                             |
| Infrastructure__AppDomain                                        | yes      | <http://receiver-api:8080>                                                            |
| OTEL_EXPORTER_OTLP_ENDPOINT                                      | no       | http://monitoring_otel:4317                                                           |

\* required for communication with altinns staging and prod systems

## Run with docker compose

Use the compose files `compose.yaml` and `compose-infra.yaml` provided in [/src](./../src/).
The `compose.yaml` contains the receiver app and the described configuration above.

```terminal
docker compose -f compose-infra.yaml up -d
```

> For the docker setup, we use the receiver image pushed to ghcr, which also includes prerelease versions. All available versions can be found [here](https://github.com/Arbeidstilsynet/receiver/pkgs/container/receiver).

## Run in nais-cluster

Use (and adjust) the provided examples in [./examples/.nais](./examples/.nais).

> For the nais setup, we publish the same receiver image to our nais repository. We do not publish prerelease versions in nais. The latest version will be in accordance with our published [releases](https://github.com/Arbeidstilsynet/receiver/releases).
