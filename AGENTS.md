# BonniePDF (.NET) - agent guide

The .NET implementation of BonniePDF. C#, targets `net8.0` (all projects in the solution - core, tests, demo, examples - target the same TFM; check `BonniePDF/BonniePDF.csproj` for the authoritative value before assuming otherwise), zero native dependencies, no GDI+/SkiaSharp. Published on NuGet as `BonniePDF`.

For the cross-project picture, see [`../AGENTS.md`](../AGENTS.md). The TypeScript implementation lives at [`../BonniePDF-TypeScript/`](../BonniePDF-TypeScript/).

## What BonniePDF is

BonniePDF generates PDF documents from code using an object-composition layout model: you build a tree of elements (text, tables, lists, images, shapes, charts), and the engine measures, flows, and paginates them into PDF bytes. It is **not** an HTML-to-PDF engine, **not** a PDF parser, and **not** a form filler. Pure managed code, no native binaries.

## The parity principle (read this first)

**BonniePDF is one library with two co-equal, first-class implementations.** .NET and TypeScript are peers. Neither is "the reference" the other ports from; neither chases the other's source. Both implement the same **language-neutral specification** ([`the shared specification`](the shared specification) — the document model, the layout/pagination algorithm, the element catalogue, the compliance rules, and the expected PDF output). The spec is the source of truth; the two codebases are siblings that conform to it.

This governs almost every decision in this repo:

1. **Behavioural parity is the contract.** The same logical document produces the same PDF output in both implementations — byte-for-byte where the spec defines bytes (object structure, content streams, XMP, font subsetting), modulo the platform-mandated differences below. The **shared conformance suite** verifies this, not a reading of the TypeScript source. If .NET and TS disagree on output for equivalent input, one has a bug against the spec.

2. **The API surface is idiomatic, not transliterated.** C# code must read like C#. We present PascalCase members, real method overloads, indexers, named arguments, and the fluent `Pdf.Create` API because those are what .NET developers expect. We do **not** reshape the surface to match TypeScript's spelling, and TypeScript does not reshape to match ours. A concrete, correct example of the same behaviour spelled idiomatically per language: an `AddRow(TableRow)` **overload** is right in C# (the language has real overloading); the TypeScript side expresses the same capability as a single union-typed `addRow(...)` because TS overloads are awkward. Same behaviour, idiomatic spelling. That divergence is the design, not a defect.

3. **Features land in lockstep against the spec.** A capability is "done" only when it exists in the spec, in both implementations, and in the shared conformance suite. Adding a feature to one language only is a parity break — flag it, don't ship it silently.

When you find a difference between this code and TypeScript, classify it before acting: **(a)** platform-mandated (allowed — see below), **(b)** idiomatic-surface (allowed — same behaviour, different spelling), or **(c)** behavioural divergence (a bug in one implementation). Only (c) is a defect. Don't "fix" (a) or (b) by transliterating TypeScript into C#.

## Platform-mandated differences (allowed by the spec)

These exist because the runtimes differ, not because one side is behind:

- **Synchronous generation is fully supported here.** .NET has no browser sandbox: `GenerateBytes()`/`Save()` are first-class, and every IO path has an async sibling (`GenerateBytesAsync`, `SaveAsync`). The TypeScript browser build is async-only for crypto/font paths — that is its platform constraint, not a .NET feature gap.
- **Palette access mechanism.** `ColorPalette.Red[500]` uses a real C# indexer. TypeScript exposes the same tokens through bracket access on a frozen record (no class indexers in TS). Same tokens, same hex values, different mechanism.

## The C# surface (what idiomatic means here)

The public API targets the shape a .NET developer expects.

- **Two public APIs, one engine.** A fluent `Pdf.Create(doc => …)` API (namespace `BonniePDF.Fluent`) and a traditional `new Document(); doc.Add(…); doc.Save(…)` API. **Both must produce byte-identical output for equivalent input** — adding a feature to one without the other breaks the contract.
- **PascalCase** for all public members; **real overloads** where they read well; **indexers** for palette access; **named/optional arguments** rather than faked overload ladders when a call has many knobs.
- **Async first-class.** Every save/generate path has an async sibling. New IO follows suit.
- **No native binaries.** Pure managed code — no `SkiaSharp`, `System.Drawing`, ImageMagick, or libpdfium. The no-deps promise is a selling point.
- **Standard-14 fonts work without embedding**; any other (TrueType) must be embedded with subsetting. PDF/A requires embedding even the standard 14.

**Reciprocal cleanup obligation.** The TypeScript surface is being brought up to standard (see [`the shared specification`](the shared specification)). The parity principle is symmetric: when that work redefines a concept's canonical name or input shape in the spec, audit the C# surface for the *idiomatic C# expression* of the same concept — and conversely, never let a TypeScript-ism (camelCase members, union-typed pseudo-overloads, options bags where C# wants real overloads or named args) leak into this codebase to "match." Idiomatic divergence in spelling is the goal; behavioural parity is the invariant.

## Engineering invariants (spec-level — both implementations honour these)

These are correctness requirements of the format and the shared spec, not C# quirks.

- **PDF/A is opt-in via `doc.Settings.Conformance`** (`PdfAConformance`: `None`, `PdfA1b`, `PdfA1a`, `PdfA2b`, `PdfA2a`, `PdfA3b`, `PdfA3a`, `PdfA4`). The legacy `PdfAEnabled = true` boolean maps to `PdfA2b`. Any non-`None` value auto-emits sRGB ICC profile + XMP + OutputIntent — don't also write them manually. The serializer auto-upgrades `Version`: PDF/A-1/2/3 → `Pdf17`, PDF/A-4 → `Pdf20`.
- **PDF/A-4 is the PDF 2.0 baseline (ISO 19005-4:2020).** No a/b/u suffix — a single `PdfA4` value; XMP emits `pdfaid:part 4` + `pdfaid:rev 2020` with **no** `pdfaid:conformance` (see [`XmpConformanceBlock.PdfA4`](BonniePDF/Metadata/XmpConformanceBlock.cs)). `Settings.AsPdfA4()` selects it and forces `Pdf20`. PDF/A-4f and PDF/A-4e are **not** implemented (roadmap: `the specification` in BonniePDF).
- **PDF/UA-2 is not implemented.** `AccessibilityMode` has only `None`/`PdfUa1`; the structure tree is `Pdf17`-only. Requesting `PdfUa1` on a `Pdf20` document **throws** rather than silently dropping tagging — the honest-claim guard until the PDF 2.0 tagging model lands. Don't "fix" by downgrading.
- **"a" levels require tagged output.** `PdfA1a`/`PdfA2a`/`PdfA3a` throw at save time unless `Settings.Accessibility = AccessibilityMode.PdfUa1`. The "b" levels and `PdfA4` have no such requirement.
- **PDF/A + PDF/UA emits an XMP extension schema.** When both are claimed, [`XmpMetadataWriter`](BonniePDF/Metadata/XmpMetadataWriter.cs) appends a `pdfaExtension:schemas` bag describing the `pdfuaid` namespace (ISO 19005-2/3 §6.6.2.3). Don't strip it — veraPDF `2a`+ fails without it.
- **Honest-claim guard.** When standard-14 fonts slip through unembedded, XMP/OutputIntent are emitted from a settings clone with `Conformance` reset to `None`, suppressing a claim the output can't honour. Keep it.
- **Resources live on the page, not the `/Pages` parent** (PDF/A-2b clause 6.2.2.2). Don't "fix" this to inheritance.
- **`ColorPalette` is the single source of truth for palette hex values.** `Color` is just the RGBA primitive (`Black`/`White`/`Red`/`FromHex`/…); it does **not** carry `Slate800`/`Red500`. Access shades via `ColorPalette.Red[500]`. The hex values are the exact Tailwind v4 swatches, hand-tuned in OKLCH, **not** computed from a base colour. Don't introduce arbitrary hex-string colour parameters on public APIs without routing them through the palette story.
- **Document mutation after `Save()` is unsupported.** The serializer assumes a frozen document tree. A feature needing replay needs explicit support.

## Examples and tests

- `Examples/` holds isolated example projects (SimpleDocument, BusinessInvoice, ChartShowcase, …). They double as smoke tests of the public surface and must use the idiomatic front-door API (both the fluent and traditional forms where it illustrates parity).
- `BonniePDF.Tests/` is the xUnit suite. It and the TypeScript vitest suite are both derived from the **shared conformance fixtures**, not from each other. Change behaviour → update the fixtures and both suites together; that is how parity is enforced.
- `BonniePDF.Validation/` runs output through veraPDF and other validators for PDF/A conformance. Don't disable validation cases to make a change pass.

## When in doubt

- The **spec** is the authority, not the other language's source. If a change here would force a behavioural divergence from the spec (and therefore from TypeScript), surface it — it likely needs a spec change applied to both implementations.
- `README.md` and `docs/` are the authoritative user-facing description. Keep them in sync with public-API changes.
