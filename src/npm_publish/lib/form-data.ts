// Hand-written (not generated): multipart body serializer matching ASP.NET Core form binding.
//
// hey-api's default formDataBodySerializer JSON-encodes object fields, so
// `Metadata: { orgnr: "1" }` is sent as a single `Metadata` field, which the receiver
// binds to an empty dictionary. ASP.NET expects `Metadata[orgnr]=1` instead.

const isPlainObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" &&
  value !== null &&
  !(value instanceof Blob) &&
  !(value instanceof Date) &&
  !Array.isArray(value);

const append = (data: FormData, key: string, value: unknown): void => {
  if (value === undefined || value === null) {
    return;
  }
  if (value instanceof Blob || typeof value === "string") {
    data.append(key, value);
  } else if (value instanceof Date) {
    data.append(key, value.toISOString());
  } else if (Array.isArray(value)) {
    value.forEach((item, index) =>
      // Files and scalars repeat the key (binds List<IFormFile>/List<string>); objects need an index.
      append(data, isPlainObject(item) ? `${key}[${index}]` : key, item),
    );
  } else if (isPlainObject(value)) {
    Object.entries(value).forEach(([childKey, childValue]) =>
      append(data, `${key}[${childKey}]`, childValue),
    );
  } else {
    data.append(key, String(value));
  }
};

/**
 * Serializes a multipart request body the way the receiver's ASP.NET Core form binding
 * expects: nested objects as bracketed keys (`Metadata[key]`), arrays as repeated keys and
 * `Blob`/`File` values as file parts.
 *
 * Spread it into multipart SDK calls, e.g.
 * `postMeldinger({ client, body, ...receiverFormDataBodySerializer })`.
 */
export const receiverFormDataBodySerializer = {
  bodySerializer: (body: unknown): FormData => {
    const data = new FormData();
    Object.entries(body as Record<string, unknown>).forEach(([key, value]) =>
      append(data, key, value),
    );
    return data;
  },
};
