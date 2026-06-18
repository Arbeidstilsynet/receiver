# @arbeidstilsynet/receiver-types

TypeScript types for the Arbeidstilsynet **receiver** API, generated from its
OpenAPI specification. This package ships **types only** - there is no runtime
code - so it adds nothing to your bundle.

Use it together with [`openapi-fetch`](https://openapi-ts.dev/openapi-fetch/) to
get a fully type-safe HTTP client: request paths, query parameters, request
bodies and responses are all checked against the live API contract.

## Installation

```sh
npm install @arbeidstilsynet/receiver-types openapi-fetch
# or
pnpm add @arbeidstilsynet/receiver-types openapi-fetch
```

> The package is published alongside receiver releases and uses the receiver
> version, so a new version means the API contract may have changed.

## What's exported

The generated module exposes the standard `openapi-typescript` shapes:

- `paths` - every endpoint, keyed by URL, with its methods, params and responses.
- `components` - reusable schemas (DTOs), under `components["schemas"]`.
- `operations` - operations keyed by `operationId`.

## Creating a client

```ts
import createClient from "openapi-fetch";
import type { paths } from "@arbeidstilsynet/receiver-types";

export const client = createClient<paths>({
  baseUrl: "/api/receiver",
});
```

## Pulling out schema types

Alias the schemas you use so they read nicely in your code:

```ts
import type { components } from "@arbeidstilsynet/receiver-types";

export type Melding = components["schemas"]["Melding"];
export type ConsumerManifest = components["schemas"]["ConsumerManifest"];
export type GetAllMeldingerResponse =
  components["schemas"]["GetAllMeldingerResponse"];
```

## Usage examples

These mirror how the types are consumed inside the Arbeidstilsynet frontends.

### A first request

A minimal `meldinger` request - fetch the first page:

```ts
const { data, error } = await client.GET("/meldinger", {
  params: {
    query: {
      pageNumber: 1,
      pageSize: 20,
    },
  },
});

if (error) throw new Error(JSON.stringify(error));
console.log(data?.items);
```

### A typed, reusable subscriptions function

The subscriptions endpoint exposes registered receiver consumers:

```ts
import type { components } from "@arbeidstilsynet/receiver-types";

type ConsumerManifest = components["schemas"]["ConsumerManifest"];

export async function getSubscriptions(): Promise<ConsumerManifest[]> {
  const { data, error } = await client.GET("/subscriptions");

  if (error) {
    throw new Error(`Failed to get subscriptions: ${JSON.stringify(error)}`);
  }
  if (!data) {
    throw new Error("Failed to get subscriptions: empty response");
  }
  return data;
}
```

Because `ConsumerManifest` is a generated type, your editor autocompletes every
available field and flags invalid combinations at compile time. Changing the API
contract (and updating this package) surfaces breaking changes the same way.

## License

MIT. See [LICENSE](./LICENSE) for details.
