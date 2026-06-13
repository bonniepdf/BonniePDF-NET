# BonniePDF (.NET) - agent guide

The **reference implementation** of BonniePDF. C#, .NET 10+, zero native dependencies, no GDI+. Distributed on NuGet as `BonniePDF`.

For the cross-project picture (the TypeScript port, the website, the licence model), see [`../AGENTS.md`](../AGENTS.md).

## Scope

- **In scope:** generating PDFs from C# code - text, tables, lists, images, charts (bar / grouped bar / line / area / pie / lollipop / dot plot), watermarks, headers/footers, AES-256 encryption, PDF/A archival output (A-1b through A-3a, plus the PDF/A-4 baseline on PDF 2.0), PDF/UA-1 tagged output. PDF 2.0 (`PdfVersion.Pdf20`) is supported as a writer target.
- **Out of scope:** HTML-to-PDF, reading or parsing existing PDFs, form filling. Don't add them.

## API conventions

- **Two public APIs, one engine.** A `Pdf.Create(doc => …)` fluent API (under `BonniePDF.Fluent`) and a traditional `new Document(); doc.Add(…); doc.Save(…)` API. **Both must produce byte-identical output for equivalent input.** Adding a feature to one without the other breaks the contract.
- **Async first-class.** Every save / generate path has an async sibling (`SaveAsync`, `GenerateBytesAsync`). New IO paths should follow suit.
- **No native binaries.** Pure managed code. Don't pull in `SkiaSharp`, `System.Drawing`, ImageMagick, libpdfium, etc. - the no-deps promise is a selling point.
- **Standard-14 fonts work without embedding**, but anything else (TrueType) must be embedded with subsetting. PDF/A output requires embedding even the standard 14.

## Gotchas

- **PDF/A is opt-in via `doc.Settings.Conformance`** (enum `PdfAConformance`: `None`, `PdfA1b`, `PdfA1a`, `PdfA2b`, `PdfA2a`, `PdfA3b`, `PdfA3a`, `PdfA4`). The legacy `PdfAEnabled = true` boolean still works and maps to `PdfA2b` (the modern visual-reproducibility baseline). When any non-None value is set, sRGB ICC profile + XMP metadata + OutputIntent are emitted automatically - do not also write them manually. The serializer auto-upgrades `Version`: PDF/A-1/2/3 → `Pdf17`, **PDF/A-4 → `Pdf20`**.
- **PDF/A-4 is the PDF 2.0 baseline (ISO 19005-4:2020).** It has no a/b/u suffix - there is a single `PdfA4` value, and its XMP emits `pdfaid:part 4` + `pdfaid:rev 2020` with **no** `pdfaid:conformance` (see [`XmpConformanceBlock.PdfA4`](BonniePDF/Metadata/XmpConformanceBlock.cs)). `Settings.AsPdfA4()` selects it and forces `Pdf20`. PDF/A-4f (embedded files) and PDF/A-4e (engineering) are **not** implemented. This mirrors the TypeScript port; see `the specification` (in BonniePDF) for the roadmap.
- **PDF/UA-2 is not implemented.** `AccessibilityMode` has only `None`/`PdfUa1`, and the structure tree is emitted for `Pdf17` only. Requesting `AccessibilityMode.PdfUa1` on a `Pdf20` document **throws** rather than silently dropping the tagging - don't "fix" this by downgrading; it's the honest-claim guard until the PDF 2.0 tagging model lands.
- **"a" levels require tagged output.** `PdfA1a` / `PdfA2a` / `PdfA3a` throw at save time unless `Settings.Accessibility = AccessibilityMode.PdfUa1` is also set. The "b" levels and the `PdfA4` baseline have no such requirement.
- **PDF/A + PDF/UA emits an XMP extension schema.** When both are claimed, [`XmpMetadataWriter`](BonniePDF/Metadata/XmpMetadataWriter.cs) appends a `pdfaExtension:schemas` bag describing the `pdfuaid` namespace (ISO 19005-2/3 §6.6.2.3). Don't strip it - veraPDF profile `2a` and above will fail without it.
- **Resources live on the page, not the parent.** Each `/Page` carries its own `/Resources` indirect reference (PDF/A-2b clause 6.2.2.2). The `/Pages` parent does not. Don't "fix" this back to inheritance.
- **Tailwind palette tokens live on `ColorPalette`, not `Color`.** `Color` is just the RGBA primitive (`Black`/`White`/`Red`/`FromHex`/…); it does **not** carry `Slate800`/`Red500` etc. Access shades via the indexer: `ColorPalette.Red[500]`. `ColorPalette` is **package-supplied tokens**, not arbitrary CSS strings — don't introduce hex-string colour parameters on public APIs without checking how they interact with the palette. `ColorPalette` is the **single source of truth** for the hex values; they are the exact Tailwind v4 swatches and are intentionally **not** computed from a base colour (Tailwind ramps are hand-tuned in OKLCH). The TS port mirrors this as bracket access on a frozen `Record` (no class indexers in TS) — keep the two in step.
- **Document mutation after `Save()` is unsupported.** The serializer assumes a frozen document tree. If a feature needs replay, it needs explicit support.

## Examples and tests

- `Examples/` holds isolated example projects (SimpleDocument, BusinessInvoice, ChartShowcase, etc.). They double as smoke tests for the public API surface.
- `BonniePDF.Tests/` is the xUnit suite. The TypeScript port's vitest suite was derived from this - if you change behaviour here, the TS port likely needs the matching update.
- `BonniePDF.Validation/` runs output through veraPDF and other validators for PDF/A conformance. Don't disable validation cases to make a change pass.

## When in doubt

- The .NET API is the **spec** the TS port follows. If a change here would force a divergence in TypeScript, surface it explicitly.
- `README.md` and `docs/` are the authoritative user-facing description of what the library does. Keep them in sync with public-API changes.
