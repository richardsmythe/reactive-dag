import {
  mkdirSync,
  readFileSync,
  readdirSync,
  rmSync,
  statSync,
  writeFileSync,
} from "node:fs";
import { execFileSync } from "node:child_process";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const scriptDir = dirname(fileURLToPath(import.meta.url));
const docsDir = resolve(scriptDir, "..");
const repoRoot = resolve(docsDir, "..");
const sourceRoot = join(repoRoot, "reactivedag");
const outputRoot = join(docsDir, "generated");
const repository = "https://github.com/richardsmythe/reactive-dag";
const commit =
  process.env.SOURCE_COMMIT ||
  execFileSync("git", ["rev-parse", "HEAD"], {
    cwd: repoRoot,
    encoding: "utf8",
  }).trim();

const pageDefinitions = [
  {
    slug: "dag-engine",
    title: "DagEngine",
    description:
      "Create, update, inspect, stream, and remove nodes in a reactive directed acyclic graph.",
    matches: (path) => path.endsWith("Engine/DagEngine.cs"),
  },
  {
    slug: "pipeline-builder",
    title: "DagPipelineBuilder",
    description:
      "Build typed and mixed-type pipelines through ReactiveDAG's fluent API.",
    matches: (path) => path.endsWith("Engine/Builder.cs"),
  },
  {
    slug: "cells-and-nodes",
    title: "Cells and nodes",
    description:
      "Values, computations, subscriptions, and lazy evaluation primitives.",
    matches: (path) =>
      [
        "Models/BaseCell.cs",
        "Models/Cell.cs",
        "Models/DagNode.cs",
        "Models/DagNodeBase.cs",
      ].some((suffix) => path.endsWith(suffix)),
  },
  {
    slug: "contracts-and-enums",
    title: "Contracts and enums",
    description:
      "Shared interfaces, DTOs, statuses, and public model contracts.",
    matches: (path) => path.includes("Models/"),
  },
];

function walk(directory) {
  return readdirSync(directory)
    .flatMap((name) => {
      const path = join(directory, name);
      return statSync(path).isDirectory() ? walk(path) : [path];
    })
    .filter((path) => path.endsWith(".cs"))
    .filter((path) => !path.endsWith("Program.cs"))
    .sort();
}

function countBraces(line) {
  const withoutLineComment = line.replace(/\/\/.*$/, "");
  const withoutStrings = withoutLineComment
    .replace(/@?"(?:""|\\.|[^"\\])*"/g, "")
    .replace(/'(?:\\.|[^'\\])'/g, "");
  return [...withoutStrings].reduce(
    (total, character) =>
      total + (character === "{" ? 1 : character === "}" ? -1 : 0),
    0,
  );
}

function findTypeRanges(lines) {
  const ranges = [];
  const typePattern =
    /^\s*(public|internal|private|protected)?\s*(?:(?:abstract|sealed|static|partial|readonly)\s+)*(class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)/;

  for (let index = 0; index < lines.length; index += 1) {
    const match = lines[index].match(typePattern);
    if (!match) continue;

    let openLine = index;
    while (openLine < lines.length && !lines[openLine].includes("{")) {
      openLine += 1;
    }
    if (openLine === lines.length) continue;

    let depth = 0;
    let closeLine = openLine;
    for (; closeLine < lines.length; closeLine += 1) {
      depth += countBraces(lines[closeLine]);
      if (depth === 0) break;
    }

    ranges.push({
      name: match[3],
      access: match[1] || "internal",
      start: index,
      end: closeLine,
    });
  }

  return ranges;
}

function cleanXmlComment(lines, declarationLine) {
  const commentLines = [];
  let cursor = declarationLine - 1;

  while (cursor >= 0 && /^\s*\[.*\]\s*$/.test(lines[cursor])) cursor -= 1;
  while (cursor >= 0 && /^\s*\/\/\//.test(lines[cursor])) {
    commentLines.unshift(lines[cursor].replace(/^\s*\/\/\/\s?/, ""));
    cursor -= 1;
  }

  if (commentLines.length === 0) return "";

  const xml = commentLines.join(" ");
  const summary = xml.match(/<summary>([\s\S]*?)<\/summary>/)?.[1] || xml;
  return summary
    .replace(/<see\s+cref="(?:[A-Z]:)?([^"}]+)"\s*\/>/g, "`$1`")
    .replace(/<typeparamref\s+name="([^"]+)"\s*\/>/g, "`$1`")
    .replace(/<paramref\s+name="([^"]+)"\s*\/>/g, "`$1`")
    .replace(/<[^>]+>/g, " ")
    .replace(/\s+/g, " ")
    .trim();
}

function readSignature(lines, start) {
  const parts = [];
  let index = start;
  let parenDepth = 0;

  for (; index < lines.length; index += 1) {
    const trimmed = lines[index].trim();
    parts.push(trimmed);
    parenDepth += [...trimmed].filter((char) => char === "(").length;
    parenDepth -= [...trimmed].filter((char) => char === ")").length;

    const joined = parts.join(" ");
    const terminates =
      parenDepth <= 0 &&
      (joined.includes("{") || joined.includes("=>") || joined.endsWith(";"));
    if (terminates) break;
  }

  return parts
    .join(" ")
    .replace(/\s+/g, " ")
    .replace(/\s*\{.*$/, "")
    .replace(/\s*=>.*$/, "")
    .replace(/;$/, "")
    .trim();
}

function symbolName(signature) {
  const delegate = signature.match(
    /\bdelegate\s+\S+\s+([A-Za-z_][A-Za-z0-9_]*)(?:<[^>]+>)?\s*\(/,
  );
  if (delegate) return delegate[1];

  const type = signature.match(
    /\b(?:class|interface|struct|enum)\s+([A-Za-z_][A-Za-z0-9_]*)/,
  );
  if (type) return type[1];

  const parameterIndex = signature.indexOf("(");
  if (parameterIndex >= 0) {
    const beforeParameters = signature.slice(0, parameterIndex);
    const method = beforeParameters.match(
      /([A-Za-z_][A-Za-z0-9_]*)(?:<[^>]+>)?\s*$/,
    );
    if (method) return method[1];
  }

  const property = signature.match(/([A-Za-z_][A-Za-z0-9_]*)\s*$/);
  return property?.[1] || "API member";
}

function declarationsFor(file) {
  const text = readFileSync(file, "utf8").replace(/^\uFEFF/, "");
  const lines = text.split(/\r?\n/);
  const ranges = findTypeRanges(lines);
  const path = relative(repoRoot, file).replaceAll("\\", "/");
  const declarations = [];

  for (let index = 0; index < lines.length; index += 1) {
    if (!/^\s*public\s+/.test(lines[index])) continue;

    const containingTypes = ranges
      .filter((range) => range.start < index && index < range.end)
      .sort((a, b) => a.start - b.start);
    if (containingTypes.some((range) => range.access !== "public")) continue;

    const ownType = ranges.find((range) => range.start === index);
    if (ownType && ownType.access !== "public") continue;

    const signature = readSignature(lines, index);
    if (!signature || signature === "public") continue;

    const name = symbolName(signature);
    const parent = containingTypes.at(-1)?.name;
    const isConstructor =
      parent &&
      name === parent &&
      signature.includes("(") &&
      !/\b(?:class|interface|struct|enum)\s+/.test(signature);
    declarations.push({
      file: path,
      line: index + 1,
      name,
      qualifiedName:
        parent && (parent !== name || isConstructor)
          ? `${parent}.${name}`
          : name,
      signature,
      summary: cleanXmlComment(lines, index),
      sourceUrl: `${repository}/blob/${commit}/${path}#L${index + 1}`,
    });
  }

  return declarations;
}

function markdownForPage(page, symbols) {
  const nameCounts = new Map();
  for (const symbol of symbols) {
    nameCounts.set(
      symbol.qualifiedName,
      (nameCounts.get(symbol.qualifiedName) || 0) + 1,
    );
  }
  const seen = new Map();

  const body = symbols
    .map((symbol) => {
      const occurrence = (seen.get(symbol.qualifiedName) || 0) + 1;
      seen.set(symbol.qualifiedName, occurrence);
      const overloaded = nameCounts.get(symbol.qualifiedName) > 1;
      const heading = overloaded
        ? `${symbol.qualifiedName} (overload ${occurrence})`
        : symbol.qualifiedName;
      const summary =
        symbol.summary ||
        "This public member currently has no XML summary in the source snapshot.";

      return [
        `## \`${heading}\``,
        "",
        "```csharp",
        symbol.signature,
        "```",
        "",
        summary,
        "",
        `[View source at \`${symbol.file}:${symbol.line}\`](${symbol.sourceUrl})`,
      ].join("\n");
    })
    .join("\n\n");

  return [
    "---",
    `title: ${page.title}`,
    `description: ${page.description}`,
    "---",
    "",
    `Generated from commit [\`${commit.slice(0, 12)}\`](${repository}/commit/${commit}).`,
    "Every declaration below links to its immutable source line.",
    "",
    body,
    "",
  ].join("\n");
}

const files = walk(sourceRoot);
const declarations = files.flatMap(declarationsFor);
const claimedFiles = new Set();
const pages = pageDefinitions.map((page, pageIndex) => {
  const symbols = declarations.filter((symbol) => {
    if (!page.matches(symbol.file)) return false;
    if (pageIndex === pageDefinitions.length - 1 && claimedFiles.has(symbol.file)) {
      return false;
    }
    return true;
  });
  symbols.forEach((symbol) => claimedFiles.add(symbol.file));
  return { ...page, symbols };
});

rmSync(outputRoot, { recursive: true, force: true });
mkdirSync(outputRoot, { recursive: true });

for (const page of pages) {
  writeFileSync(
    join(outputRoot, `${page.slug}.md`),
    markdownForPage(page, page.symbols),
  );
}

const intro = [
  "---",
  "title: ReactiveDAG API reference",
  "description: Source-linked reference for ReactiveDAG's public .NET API.",
  "---",
  "",
  "ReactiveDAG is a reactive directed acyclic graph engine for .NET 8.",
  "This reference complements the hand-written architecture and examples in the project README.",
  "",
  "## Snapshot",
  "",
  `- Repository: [\`richardsmythe/reactive-dag\`](${repository})`,
  `- Commit: [\`${commit}\`](${repository}/commit/${commit})`,
  `- Public declarations indexed: **${declarations.length}**`,
  `- Source files indexed: **${files.length}**`,
  "- Generator: deterministic C# source adapter plus Sourcey 3.6.5",
  "",
  "## Reference sections",
  "",
  ...pages.map(
    (page) =>
      `- [${page.title}](generated/${page.slug}) — ${page.symbols.length} public declarations`,
  ),
  "",
  "## Rebuild",
  "",
  "```bash",
  "cd docs",
  "npm ci",
  "npm run build",
  "npm run verify",
  "```",
  "",
  "The generated pages and search index are reproducible from the pinned source snapshot.",
  "",
].join("\n");
writeFileSync(join(outputRoot, "introduction.md"), intro);

const manifest = {
  schema: "reactivedag-sourcey-reference.v1",
  repository,
  commit,
  generated_with: "sourcey@3.6.5",
  source_files: files.map((file) =>
    relative(repoRoot, file).replaceAll("\\", "/"),
  ),
  symbol_count: declarations.length,
  pages: pages.map((page) => ({
    slug: page.slug,
    title: page.title,
    symbol_count: page.symbols.length,
  })),
  symbols: declarations.map(({ qualifiedName, file, line, sourceUrl }) => ({
    name: qualifiedName,
    file,
    line,
    source_url: sourceUrl,
  })),
};
writeFileSync(
  join(outputRoot, "manifest.json"),
  `${JSON.stringify(manifest, null, 2)}\n`,
);

console.log(
  `Generated ${manifest.symbol_count} public declarations across ${pages.length} reference pages at ${commit.slice(0, 12)}.`,
);
