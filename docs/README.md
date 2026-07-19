# Generated API documentation

This directory turns ReactiveDAG's public C# surface and XML comments into a
searchable, source-linked documentation site with Sourcey 3.6.5. Runtime code is
not modified by the documentation build.

## Build locally

Node.js 20 or newer is required.

```bash
cd docs
npm ci
npm run build
npm run verify
```

`npm run build` regenerates the Markdown under `generated/` before rendering the
static site to the ignored `dist/` directory. `npm run verify` checks the public
API count, immutable line-level source links, search coverage, project Pages
paths, and context exports.

The generator uses the current Git commit for source links. Set
`SOURCE_COMMIT=<sha>` only when intentionally documenting a different immutable
snapshot.

## Deployment

Pull requests that affect the C# API or docs run the build and verification.
After a matching change reaches `master`, the `Sourcey API docs` workflow
publishes the verified site to the repository's GitHub Pages environment.

The repository owner must enable Pages with **GitHub Actions** as the source once
under **Settings → Pages**. This keeps the workflow on the default GitHub token
instead of requiring a separate administration-scoped token.
