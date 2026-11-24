# Architecture

The `receiver` service is designed to be used as a publisher, which means it requires a set of consumers to work as intended. The workflow goes as follows:

1) a `consumer` notifies the `receiver` with a so called `ConsumerManifest`, which basically contains a list of sources the consumer is interested in.
2) the `receiver` registers the consumer, and (if necessary) also sets up a subscription to an altinn app
3) the `receiver` will from now on be ready to receive messages / documents from these sources and:

- process these messages (virus scan / document storage / persistence of structured data)
- notify all consumers if a new message is available

4) each `consumer` is (technically) responsible on its own to read / consume messages from valkey. But the `Arbeidstilsynet.Receiver` nuget package will provide convenience methods and extensions to fix this.

The following diagram illustrates with which kind of external services the **receiver** app communicates:

![architecture](./diagrams/architecture.svg)

## Event driven `Melding` distribution

![valkey](./diagrams/stream.svg)
## Document soft deletion

Only attachments can be soft-deleted, for every application and message source. Deleting
main content or structured data returns `400 Bad Request`. The database row, metadata and
stored file are kept, nothing is deleted in Altinn, and no new message notification is published.

- `DELETE /meldinger/{meldingId}/documents/{documentId}` soft-deletes an attachment.
- `POST /meldinger/{meldingId}/documents/{documentId}/restore` restores it.
- `GET /meldinger/{meldingId}/documents/deleted` lists soft-deleted attachments with `deletedAt`.

Delete and restore are idempotent and return `204 No Content`, or `404` when the document does
not belong to the melding. Soft-deleted attachments are hidden from every other read: melding
lookups and listings (`attachmentIds`), document listings, document metadata and downloads.
Restore an attachment before downloading it.
These operations rely on cluster network access restrictions, not endpoint authentication.
