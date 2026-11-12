// Runs against the built package:
//   pnpm --filter @arbeidstilsynet/receiver build && pnpm --filter @arbeidstilsynet/receiver test
// Set UPDATE_FIXTURE=1 to rewrite the multipart fixture replayed by the App tests
// (src/App/test/PostMeldingBodyFormBindingTests.cs).
import assert from "node:assert/strict";
import { readFile, writeFile } from "node:fs/promises";
import { test } from "node:test";

import { postMeldinger } from "../dist/index.js";
import { createClient, createConfig } from "../dist/client/index.js";

const fixtureUrl = new URL(
  "../../App/test/fixture/post-meldinger.multipart",
  import.meta.url,
);
const fixtureBoundary = "receiver-fixture-boundary";

async function capturePostMeldinger() {
  let captured;
  const client = createClient(
    createConfig({
      baseUrl: "http://receiver.test",
      fetch: async (request) => {
        captured = request;
        return Response.json({
          meldingId: "00000000-0000-0000-0000-000000000000",
        });
      },
    }),
  );
  const { error } = await postMeldinger({
    client,
    body: {
      ApplicationId: "ulykkesvarsel",
      Metadata: { orgnr: "123456789", kommune: "Oslo" },
      MainContent: new File(["%PDF"], "melding.pdf", {
        type: "application/pdf",
      }),
      Attachments: [
        new File(["a"], "a.txt", { type: "text/plain" }),
        new File(["b"], "b.txt", { type: "text/plain" }),
      ],
    },
  });
  assert.equal(error, undefined);
  return captured;
}

test("postMeldinger sends Metadata as one JSON field and Attachments as repeated parts", async () => {
  const request = await capturePostMeldinger();
  const form = await request.formData();

  assert.equal(form.get("ApplicationId"), "ulykkesvarsel");
  assert.deepEqual(form.getAll("Metadata"), [
    JSON.stringify({ orgnr: "123456789", kommune: "Oslo" }),
  ]);
  assert.equal(
    [...form.keys()].some((key) => key.startsWith("Metadata[")),
    false,
  );
  assert.equal(form.get("MainContent").name, "melding.pdf");
  assert.deepEqual(
    form.getAll("Attachments").map((file) => file.name),
    ["a.txt", "b.txt"],
  );
});

test("postMeldinger multipart body matches the fixture replayed by the App tests", async () => {
  const request = await capturePostMeldinger();
  const boundary = /boundary=(.+)$/.exec(request.headers.get("content-type"))[1];
  const body = (await request.text())
    .replaceAll(boundary, fixtureBoundary)
    .replaceAll("\r\n", "\n");

  if (process.env.UPDATE_FIXTURE === "1") {
    await writeFile(fixtureUrl, body);
  }
  const fixture = (await readFile(fixtureUrl, "utf8")).replaceAll("\r\n", "\n");
  assert.equal(body, fixture);
});
