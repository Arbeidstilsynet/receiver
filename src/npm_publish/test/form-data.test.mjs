// Runs against the built package: pnpm build:npm && pnpm test:npm
import assert from "node:assert/strict";
import { test } from "node:test";

import { postMeldinger } from "../dist/index.js";
import { createClient, createConfig } from "../dist/client/index.js";
import { receiverFormDataBodySerializer } from "../dist/form-data.js";

const body = {
  ApplicationId: "ulykkesvarsel",
  Metadata: { orgnr: "123456789", kommune: "Oslo" },
  MainContent: new File(["%PDF"], "melding.pdf", { type: "application/pdf" }),
  Attachments: [
    new File(["a"], "a.txt", { type: "text/plain" }),
    new File(["b"], "b.txt", { type: "text/plain" }),
  ],
};

async function captureFormData(options) {
  let captured;
  const client = createClient(
    createConfig({
      baseUrl: "http://receiver.test",
      fetch: async (request) => {
        captured = await request.formData();
        return Response.json({ meldingId: "00000000-0000-0000-0000-000000000000" });
      },
    }),
  );
  await postMeldinger({ client, body, ...options });
  return captured;
}

test("postMeldinger with receiverFormDataBodySerializer sends Metadata as bracketed keys", async () => {
  const form = await captureFormData(receiverFormDataBodySerializer);

  assert.equal(form.get("ApplicationId"), "ulykkesvarsel");
  assert.equal(form.get("Metadata[orgnr]"), "123456789");
  assert.equal(form.get("Metadata[kommune]"), "Oslo");
  assert.equal(form.has("Metadata"), false);
  assert.equal(form.get("MainContent").name, "melding.pdf");
  assert.deepEqual(
    form.getAll("Attachments").map((file) => file.name),
    ["a.txt", "b.txt"],
  );
});

test("postMeldinger default serializer sends Metadata as one JSON field (binds empty on the server)", async () => {
  const form = await captureFormData({});

  assert.equal(form.get("Metadata"), JSON.stringify(body.Metadata));
  assert.equal(form.has("Metadata[orgnr]"), false);
});
