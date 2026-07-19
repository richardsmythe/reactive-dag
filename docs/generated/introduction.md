---
title: ReactiveDAG API reference
description: Source-linked reference for ReactiveDAG's public .NET API.
---

ReactiveDAG is a reactive directed acyclic graph engine for .NET 8.
This reference complements the hand-written architecture and examples in the project README.

## Snapshot

- Repository: [`richardsmythe/reactive-dag`](https://github.com/richardsmythe/reactive-dag)
- Commit: [`254db11b060f106bbd359c0f630770d3a792e474`](https://github.com/richardsmythe/reactive-dag/commit/254db11b060f106bbd359c0f630770d3a792e474)
- Public declarations indexed: **75**
- Source files indexed: **9**
- Generator: deterministic C# source adapter plus Sourcey 3.6.5

## Reference sections

- [DagEngine](./dag-engine.md) — 15 public declarations
- [DagPipelineBuilder](./pipeline-builder.md) — 15 public declarations
- [Cells and nodes](./cells-and-nodes.md) — 38 public declarations
- [Contracts and enums](./contracts-and-enums.md) — 7 public declarations

## Rebuild

```bash
cd docs
npm ci
npm run build
npm run verify
```

The generated pages and search index are reproducible from the pinned source snapshot.
