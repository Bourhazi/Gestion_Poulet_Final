import assert from "node:assert/strict";
import { test } from "node:test";
import { createAppTheme } from "../src/theme.ts";

test("the light dashboard theme uses the approved professional palette", () => {
  const theme = createAppTheme("light");

  assert.equal(theme.palette.mode, "light");
  assert.equal(theme.palette.primary.main, "#2563eb");
  assert.equal(theme.palette.secondary.main, "#15803d");
  assert.equal(theme.palette.background.default, "#f8fafc");
});

test("the dark dashboard theme provides a dark content surface", () => {
  const theme = createAppTheme("dark");

  assert.equal(theme.palette.mode, "dark");
  assert.equal(theme.palette.background.default, "#0f172a");
  assert.notEqual(theme.palette.background.paper, theme.palette.background.default);
});
