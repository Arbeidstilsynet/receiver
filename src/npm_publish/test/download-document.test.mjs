import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const spec = JSON.parse(
  await readFile(new URL("../../generated/openApi.json", import.meta.url), "utf8"),
);

test("document download is described as a binary response", () => {
  const ok =
    spec.paths["/meldinger/{meldingId}/documents/{documentId}/download"].get
      .responses["200"];
  const schema = ok.content["application/octet-stream"].schema;
  const resolved = schema.$ref
    ? spec.components.schemas[schema.$ref.split("/").pop()]
    : schema;

  assert.deepEqual(resolved, { type: "string", format: "binary" });
});
