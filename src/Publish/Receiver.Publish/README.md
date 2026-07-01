# Arbeidstilsynet.Receiver

Client library for consuming _meldinger_ (messages/notifications) from **MeldingerReceiver**.

MeldingerReceiver receives meldinger from external sources — primarily Digdir's Altinn
platform — scans and stores them, and notifies registered consumers over a Valkey (Redis)
stream. This package lets a consumer application subscribe to that stream, react to new
meldinger, and download the structured data and documents that belong to them.

> Domain terms are kept in Norwegian on purpose (e.g. `Melding`, `ConsumerManifest`) to stay
> consistent with the naming used across the domain.

## 📦 This client integrates with a MeldingerReceiver container

This package is a **client** — it does not host the receiver itself. It talks to a running
MeldingerReceiver service, distributed as the container image
[`ghcr.io/arbeidstilsynet/receiver`](https://github.com/Arbeidstilsynet/receiver/pkgs/container/receiver).

**Use the container image tag that matches this package's version.** The NuGet package and the
container image are built and released together from the same version, so a given package
version is verified against the container image carrying the same tag:

```
Arbeidstilsynet.Receiver  vX.Y.Z   ⟷   ghcr.io/arbeidstilsynet/receiver:X.Y.Z
```

Point `MeldingerReceiverApiConfiguration.BaseUrl` at that container, and connect both the
container and this client to the same Valkey (Redis) instance. Running mismatched versions is
not supported and may break message compatibility.

## 🧑‍💻 Getting started

### 1. Register the receiver

Register MeldingerReceiver and your consumer in the service collection. Use
`AddMeldingerReceiverWithBackgroundService<T>` to also start a hosted background service that
listens on the stream and dispatches new meldinger to your consumer:

```csharp
using Arbeidstilsynet.Receiver.DependencyInjection;

services.AddMeldingerReceiverWithBackgroundService<MyMeldingerConsumer>(
    new ValkeyConfiguration { ConnectionString = "localhost:6379" },
    new MeldingerReceiverApiConfiguration { BaseUrl = "https://your-receiver-host" }
);
```

Your consumer (`T`) is registered as a scoped `IMeldingerConsumer`. If you want to wire the
dependencies yourself (for example in tests), call `AddMeldingerReceiver(...)` instead, which
registers everything except the background listener.

> `BaseUrl` must point at a MeldingerReceiver container whose tag matches this package's version
> (see [above](#-this-client-integrates-with-a-meldingerreceiver-container)), and
> `ConnectionString` must reference the same Valkey instance the container is connected to.

### 2. Implement `IMeldingerConsumer`

`IMeldingerConsumer` is the single contract you implement. It tells the receiver **what** your
consumer subscribes to (`ConsumerManifest`), **how** it should be polled, and **what to do**
when a new melding arrives (`ConsumeMelding`).

| Member             | Purpose                                                                                                                   |
| ------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| `ConsumerManifest` | Declares the consumer's unique name and which app registrations (`AppId` + `MessageSource`) it should receive meldinger for. |
| `PollInterval`     | Backup poll interval in milliseconds. Delivery is push-based, so polling is only a safety net. `null` uses the default (1h). |
| `MaxConcurrency`   | Maximum number of meldinger processed in parallel. `null` means sequential processing (1).                                |
| `ConsumeMelding`   | Called for each new `Melding`. **Throw** to signal failure — the melding may be re-driven (redelivered) later.            |

```csharp
using Arbeidstilsynet.MeldingerReceiver.Domain.Data;
using Arbeidstilsynet.Receiver.Ports;

public class MyMeldingerConsumer(
    IMeldingerAdapter meldingerAdapter,
    IOrderService orderService,
    ILogger<MyMeldingerConsumer> logger
) : IMeldingerConsumer
{
    private const string OrderAppId = "my-order-app";

    public ConsumerManifest ConsumerManifest =>
        new()
        {
            ConsumerName = "my-consumer",
            AppRegistrations =
            [
                new() { AppId = OrderAppId, MessageSource = MessageSource.Altinn },
            ],
        };

    // Use the defaults: hourly backup poll, sequential processing.
    public int? PollInterval => null;
    public int? MaxConcurrency => null;

    public async Task ConsumeMelding(Melding newMelding)
    {
        // Route on ApplicationId if your consumer handles more than one app.
        if (newMelding.ApplicationId != OrderAppId)
        {
            throw new InvalidOperationException(
                $"Unexpected ApplicationId '{newMelding.ApplicationId}' for melding {newMelding.Id}."
            );
        }

        // Download and deserialize the structured data (JSON) into your own model.
        if (await meldingerAdapter.FetchStructuredData<OrderForm>(newMelding) is not { } form)
        {
            throw new InvalidOperationException(
                $"Melding {newMelding.Id} has no structured data to process."
            );
        }

        // Do your work. Throwing here lets the receiver re-drive the melding later.
        await orderService.Process(form, newMelding.Id, newMelding.AttachmentIds);

        logger.LogInformation("Processed melding {MeldingId}", newMelding.Id);
    }
}
```

### 3. Fetch structured data with `IMeldingerAdapter`

A `Melding` carries **references** (`MainContentId`, `StructuredDataId`, `AttachmentIds`), not
the document contents themselves. Inject `IMeldingerAdapter` and call
`FetchStructuredData<T>` to download and deserialize the structured data (always JSON) into your
own type:

```csharp
Task<TStructuredData?> FetchStructuredData<TStructuredData>(Melding melding);
```

It returns `null` when the melding has no structured data, so pattern-match the result before
using it (as shown above). `TStructuredData` is your own DTO that mirrors the form's JSON shape.

## 📨 The `Melding` record

`ConsumeMelding` receives a `Melding` describing a received message:

| Property           | Type                         | Description                                                                               |
| ------------------ | ---------------------------- | ----------------------------------------------------------------------------------------- |
| `Id`               | `Guid`                       | Unique id of the melding. Assume the same id can be resubmitted (redrive).                 |
| `Source`           | `MessageSource`              | Where the melding came from (`Altinn` or `Api`).                                           |
| `ApplicationId`    | `string`                     | The app that produced the melding — use it to route between handlers.                      |
| `ReceivedAt`       | `DateTime`                   | When the melding was received.                                                             |
| `MainContentId`    | `Guid?`                      | Reference to the human-readable main document (e.g. a PDF form), if any.                   |
| `StructuredDataId` | `Guid?`                      | Reference to the structured JSON payload, if any — fetched via `FetchStructuredData<T>`.   |
| `AttachmentIds`    | `List<Guid>`                 | References to supplementary attachment documents, if any.                                  |
| `Tags`             | `Dictionary<string, string>` | Tags supplied by the sender.                                                               |
| `InternalTags`     | `Dictionary<string, string>` | Tags added internally by the receiver.                                                     |

## ♻️ Delivery and redrive semantics

- Delivery is **push-based** over a Valkey stream; the `PollInterval` job is only a backup.
- `ConsumeMelding` should be **idempotent**: a melding with the same `Id` can be delivered more
  than once.
- **Throw from `ConsumeMelding` to signal failure.** The melding is left unacknowledged and may
  be re-driven later. Only return normally once the melding has been fully handled.

If you host redrive tooling, `MapCommonRedriveEndpoints(...)` exposes endpoints to inspect and
re-drive pending messages.
