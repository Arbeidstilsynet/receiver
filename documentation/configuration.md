# Configuration

| (env) Variable Name                                          | Required | Example value                                                   |
|-------------------------------------------------------------|----------|-----------------------------------------------------------------|
| ASPNETCORE_ENVIRONMENT                                      | yes      | Development                                                     |
| ASPNETCORE_HTTP_PORT                                        | no       | "8080"                                                          |
| Infrastructure__MaskinportenConfiguration__IntegrationId    | yes*     |                                                                 |
| Infrastructure__MaskinportenConfiguration__CertificatePrivateKey | yes*  |                                                                 |
| Infrastructure__MaskinportenConfiguration__CertificateChain | yes*     |                                                                 |
| Infrastructure__PostgresConfiguration__ConnectionString    | yes      | Host=postgres-receiver;Port=5432;Database=demo_db;Username=postgres;Password=postgres |
| Infrastructure__DocumentStorageConfiguration__BaseUrl      | no       | <http://file-storage:4443/storage/v1/>                         |
| Infrastructure__DocumentStorageConfiguration__AuthRequired | no       | true                                                            |
| Infrastructure__DocumentStorageConfiguration__BucketName   | yes      | test-bucket                                                     |
| Infrastructure__ValkeyConfiguration__ConnectionString      | yes      | valkey:6379                                                     |
| Infrastructure__VirusScanConfiguration__BaseUrl            | yes      | <http://clamav-web:8080/>                                      |
| Infrastructure__AppDomain                                  | yes      | <http://receiver-api:8080>                                     |
| API__Cors__AllowedOrigins__0                               | no       | <http://localhost:3000>                                        |
| API__Cors__AllowCredentials                                | no       | false                                                           |
| OTEL_EXPORTER_OTLP_ENDPOINT                                | no       | http://monitoring_otel:4317                                    |

\* required for communication with altinns staging and prod systems
