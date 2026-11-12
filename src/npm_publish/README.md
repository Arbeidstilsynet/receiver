# @arbeidstilsynet/receiver

A typed SDK for the Arbeidstilsynet **receiver** API, generated from its OpenAPI
specification by [`@hey-api/openapi-ts`](https://heyapi.dev/). The package ships
compiled ESM JavaScript with `.d.ts` declarations and has no runtime
dependencies: the fetch client is bundled with the SDK.

Each endpoint is a function (`getMeldinger`, `getSubscriptions`, ...) whose path
parameters, query parameters, request body and response are checked against the
API contract at compile time.

## Installation

```sh
npm install @arbeidstilsynet/receiver
# or
pnpm add @arbeidstilsynet/receiver
```

> The package is published alongside receiver releases and uses the receiver
> version, so a new version means the API contract may have changed.

## What's exported

- `@arbeidstilsynet/receiver`
  - SDK functions, one per endpoint, named after method and path
    (for example `getMeldinger`, `getMeldingerByMeldingId`,
    `getMeldingerByShortIdByShortId`, `getSubscriptions`, `postSubscriptions`).
  - Schema types as plain named exports (for example `Melding`,
    `ConsumerManifest`, `GetAllMeldingerResponse`, `ProblemDetails`), plus
    per-operation `*Data`, `*Responses` and `*Errors` types.
  - `client`: a shared default client without a base URL. Prefer creating your
    own client as shown below.
- `@arbeidstilsynet/receiver/client`
  - `createClient`, `createConfig` and the client types (`Client`, `Config`,
    `RequestResult`, ...).

## Creating a client

Create one client per consumer with its own base URL, and pass it to the SDK
functions:

```ts
import { createClient, createConfig } from "@arbeidstilsynet/receiver/client";

export const receiverClient = createClient(
  createConfig({ baseUrl: "/api/receiver" }),
);
```

Use `receiverClient.interceptors.request.use(...)` to add auth headers or other
cross-cutting behaviour.

## Schema types

Schema types are regular named exports, so no aliasing is required:

```ts
import type {
  ConsumerManifest,
  GetAllMeldingerResponse,
  Melding,
} from "@arbeidstilsynet/receiver";
```

## Usage examples

### A first request

Fetch the first page of meldinger. SDK functions do not throw on HTTP errors;
check `error` (and `response`) instead:

```ts
import { getMeldinger } from "@arbeidstilsynet/receiver";

const { data, error } = await getMeldinger({
  client: receiverClient,
  query: { pageNumber: 1, pageSize: 20 },
});

if (error) throw new Error(JSON.stringify(error));
console.log(data?.items);
```

### A typed, reusable subscriptions function

```ts
import {
  getSubscriptions,
  type ConsumerManifest,
} from "@arbeidstilsynet/receiver";

export async function fetchSubscriptions(): Promise<ConsumerManifest[]> {
  const { data, error, response } = await getSubscriptions({
    client: receiverClient,
  });

  if (error) {
    throw new Error(
      `Failed to get subscriptions (${response?.status}): ${JSON.stringify(error)}`,
    );
  }
  if (!data) {
    throw new Error("Failed to get subscriptions: empty response");
  }
  return data;
}
```

### Path parameters

```ts
import { getMeldingerByMeldingId } from "@arbeidstilsynet/receiver";

const { data } = await getMeldingerByMeldingId({
  client: receiverClient,
  path: { meldingId },
});
```

### Posting a melding (multipart)

`postMeldinger` sends `multipart/form-data` and works as generated. Pass a
plain object as `body`; files are sent as file parts, `Attachments` as repeated
form fields and `Metadata` as one JSON form field:

```ts
import { postMeldinger } from "@arbeidstilsynet/receiver";

const { data, error } = await postMeldinger({
  client: receiverClient,
  body: {
    ApplicationId: "ulykkesvarsel",
    Metadata: { orgnr: "123456789" },
    MainContent: pdfFile,
    Attachments: [attachment],
  },
});
```

The receiver API accepts `Metadata` either as a single form field containing a
JSON object of string values (`Metadata={"orgnr":"123456789"}`, what the SDK
sends) or as bracketed form fields (`Metadata[orgnr]=123456789`). Sending both
forms in one request, invalid JSON or non-string values is rejected with
`400 Bad Request`.

## Migrating from `@arbeidstilsynet/receiver-types`

This package replaces the types-only `@arbeidstilsynet/receiver-types`
(`openapi-typescript` declarations used with `openapi-fetch`), which will be
deprecated. Install `@arbeidstilsynet/receiver` instead, remove
`@arbeidstilsynet/receiver-types` and `openapi-fetch`, and update your calls:

| `receiver-types` + openapi-fetch                     | `receiver`                                                         |
| ---------------------------------------------------- | ------------------------------------------------------------------ |
| `createClient<paths>({ baseUrl })` from openapi-fetch | `createClient(createConfig({ baseUrl }))` from `.../client`        |
| `client.GET("/meldinger", { params: { query } })`    | `getMeldinger({ client, query })`                                  |
| `client.GET("/meldinger/{meldingId}", { params: { path } })` | `getMeldingerByMeldingId({ client, path })`               |
| `components["schemas"]["Melding"]`                   | `Melding`                                                          |

## License

MIT. See [LICENSE](./LICENSE) for details.
