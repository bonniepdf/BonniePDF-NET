# BonniePDF (.NET) — agent guide

The **reference implementation** of BonniePDF. C#, .NET 10+, zero native dependencies, no GDI+. Distributed on NuGet as `BonniePDF`.

For the cross-project picture (the TypeScript port, the website, the licence model), see [`../AGENTS.md`](../AGENTS.md).

## Scope

- **In scope:** generating PDFs from C# code — text, tables, lists, images, charts (bar / grouped bar / line / area / pie / lollipop / dot plot), watermarks, headers/footers, AES-256 encryption, PDF/A archival output (A-1b through A-3a), PDF/UA-1 tagged output.
- **Out of scope:** HTML-to-PDF, reading or parsing existing PDFs, form filling. Don't add them.

## API conventions

- **Two public APIs, one engine.** A `Pdf.Create(doc => …)` fluent API (under `BonniePDF.Fluent`) and a traditional `new Document(); doc.Add(…); doc.Save(…)` API. **Both must produce byte-identical output for equivalent input.** Adding a feature to one without the other breaks the contract.
- **Async first-class.** Every save / generate path has an async sibling (`SaveAsync`, `GenerateBytesAsync`). New IO paths should follow suit.
- **No native binaries.** Pure managed code. Don't pull in `SkiaSharp`, `System.Drawing`, ImageMagick, libpdfium, etc. — the no-deps promise is a selling point.
- **Standard-14 fonts work without embedding**, but anything else (TrueType) must be embedded with subsetting. PDF/A output requires embedding even the standard 14.

## Gotchas

- **PDF/A is opt-in via `doc.Settings.Conformance`** (enum `PdfAConformance`: `None`, `PdfA1b`, `PdfA1a`, `PdfA2b`, `PdfA2a`, `PdfA3b`, `PdfA3a`). The legacy `PdfAEnabled = true` boolean still works and maps to `PdfA2b` (the modern visual-reproducibility baseline). When any non-None value is set, sRGB ICC profile + XMP metadata + OutputIntent are emitted automatically — do not also write them manually. The serializer auto-upgrades `Version` to `Pdf17` if needed.
- **"a" levels require tagged output.** `PdfA1a` / `PdfA2a` / `PdfA3a` throw at save time unless `Settings.Accessibility = AccessibilityMode.PdfUa1` is also set. The "b" levels have no such requirement.
- **PDF/A + PDF/UA emits an XMP extension schema.** When both are claimed, [`XmpMetadataWriter`](BonniePDF/Metadata/XmpMetadataWriter.cs) appends a `pdfaExtension:schemas` bag describing the `pdfuaid` namespace (ISO 19005-2/3 §6.6.2.3). Don't strip it — veraPDF profile `2a` and above will fail without it.
- **Resources live on the page, not the parent.** Each `/Page` carries its own `/Resources` indirect reference (PDF/A-2b clause 6.2.2.2). The `/Pages` parent does not. Don't "fix" this back to inheritance.
- **`Color.Slate800` etc. are package-supplied tokens**, not arbitrary CSS strings. Don't introduce hex-string colour parameters on public APIs without checking how it interacts with the existing palette.
- **Document mutation after `Save()` is unsupported.** The serializer assumes a frozen document tree. If a feature needs replay, it needs explicit support.

## Examples and tests

- `Examples/` holds isolated example projects (SimpleDocument, BusinessInvoice, ChartShowcase, etc.). They double as smoke tests for the public API surface.
- `BonniePDF.Tests/` is the xUnit suite. The TypeScript port's vitest suite was derived from this — if you change behaviour here, the TS port likely needs the matching update.
- `BonniePDF.Validation/` runs output through veraPDF and other validators for PDF/A conformance. Don't disable validation cases to make a change pass.

## When in doubt

- The .NET API is the **spec** the TS port follows. If a change here would force a divergence in TypeScript, surface it explicitly.
- `README.md` and `docs/` are the authoritative user-facing description of what the library does. Keep them in sync with public-API changes.
