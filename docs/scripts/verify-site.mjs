import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const docsDir = resolve(fileURLToPath(new URL("..", import.meta.url)));
const generatedDir = join(docsDir, "generated");
const distDir = join(docsDir, "dist");
const manifest = JSON.parse(
  readFileSync(join(generatedDir, "manifest.json"), "utf8"),
);
const expectedBaseUrl = "/reactive-dag/";
const expectedSiteUrl = "https://richardsmythe.github.io/reactive-dag/";

function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}

const failures = [];
if (manifest.symbol_count < 15) {
  failures.push(`expected at least 15 public declarations, got ${manifest.symbol_count}`);
}
if (manifest.pages.some((page) => page.symbol_count === 0)) {
  failures.push("one or more reference pages contain no public declarations");
}

for (const required of ["index.html", "search-index.json", "llms.txt", "llms-full.txt"]) {
  if (!existsSync(join(distDir, required))) failures.push(`missing dist/${required}`);
}

const expectedSourcePrefix =
  `https://github.com/richardsmythe/reactive-dag/blob/${manifest.commit}/`;
if (
  manifest.symbols.some(
    (symbol) =>
      !symbol.source_url.startsWith(expectedSourcePrefix) ||
      !symbol.source_url.endsWith(`#L${symbol.line}`),
  )
) {
  failures.push("one or more declarations lack an immutable line-level source link");
}

if (existsSync(join(distDir, "search-index.json"))) {
  const searchIndex = JSON.parse(
    readFileSync(join(distDir, "search-index.json"), "utf8"),
  );
  if (
    searchIndex.some(
      (entry) =>
        typeof entry.url !== "string" ||
        !entry.url.startsWith(expectedBaseUrl),
    )
  ) {
    failures.push(`search results are not scoped to ${expectedBaseUrl}`);
  }

  const indexedTitles = new Set(searchIndex.map((entry) => entry.title));
  const missingSymbols = [
    ...new Set(manifest.symbols.map((symbol) => symbol.name)),
  ].filter(
    (name) =>
      ![...indexedTitles].some(
        (title) => title === name || title.startsWith(`${name} (overload `),
      ),
  );
  if (missingSymbols.length > 0) {
    failures.push(
      `search index is missing API symbols: ${missingSymbols.slice(0, 5).join(", ")}`,
    );
  }
}

if (existsSync(distDir)) {
  const html = walk(distDir)
    .filter((path) => path.endsWith(".html"))
    .map((path) => readFileSync(path, "utf8"))
    .join("\n");
  for (const page of manifest.pages) {
    if (!html.includes(page.title)) failures.push(`site does not include ${page.title}`);
  }
  if (!html.includes(manifest.commit)) {
    failures.push("site does not expose the immutable source commit");
  }
  if (html.includes('content="/search-index.json"')) {
    failures.push("site loads search from the host root instead of a relative path");
  }
  if (!html.includes(expectedSiteUrl)) {
    failures.push("site does not expose its project-owned canonical URL");
  }
  if (html.includes("mossony.github.io")) {
    failures.push("site contains a personal preview host");
  }
}

if (failures.length > 0) {
  console.error(failures.map((failure) => `- ${failure}`).join("\n"));
  process.exitCode = 1;
} else {
  console.log(
    `Verified ${manifest.symbol_count} declarations, ${manifest.pages.length} reference pages, search, and context exports.`,
  );
}
