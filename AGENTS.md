# BonniePDF (.NET) — agent guide

The **reference implementation** of BonniePDF. C#, .NET 10+, zero native dependencies, no GDI+. Distributed on NuGet as `BonniePDF`.

For the cross-project picture (the TypeScript port, the website, the licence model), see [`../AGENTS.md`](../AGENTS.md).

## Scope

- **In scope:** generating PDFs from C# code — text, tables, lists, images, charts (bar / grouped bar / line / area / pie / lollipop / dot plot), watermarks, headers/footers, AES-256 encryption, PDF/A-1b archival output.
- **Out of scope:** HTML-to-PDF, reading or parsing existing PDFs, form filling. Don't add them.

## API conventions

- **Two public APIs, one engine.** A `Pdf.Create(doc => …)` fluent API (under `BonniePDF.Fluent`) and a traditional `new Document(); doc.Add(…); doc.Save(…)` API. **Both must produce byte-identical output for equivalent input.** Adding a feature to one without the other breaks the contract.
- **Async first-class.** Every save / generate path has an async sibling (`SaveAsync`, `GenerateBytesAsync`). New IO paths should follow suit.
- **No native binaries.** Pure managed code. Don't pull in `SkiaSharp`, `System.Drawing`, ImageMagick, libpdfium, etc. — the no-deps promise is a selling point.
- **Standard-14 fonts work without embedding**, but anything else (TrueType) must be embedded with subsetting. PDF/A output requires embedding even the standard 14.

## Gotchas

- **PDF/A-1b is opt-in** via `doc.Settings.PdfAEnabled = true`. When enabled, sRGB ICC profile + XMP metadata + OutputIntent are emitted automatically — do not also write them manually.
- **`Color.Slate800` etc. are package-supplied tokens**, not arbitrary CSS strings. Don't introduce hex-string colour parameters on public APIs without checking how it interacts with the existing palette.
- **Document mutation after `Save()` is unsupported.** The serializer assumes a frozen document tree. If a feature needs replay, it needs explicit support.

## Examples and tests

- `Examples/` holds isolated example projects (SimpleDocument, BusinessInvoice, ChartShowcase, etc.). They double as smoke tests for the public API surface.
- `BonniePDF.Tests/` is the xUnit suite. The TypeScript port's vitest suite was derived from this — if you change behaviour here, the TS port likely needs the matching update.
- `BonniePDF.Validation/` runs output through veraPDF and other validators for PDF/A conformance. Don't disable validation cases to make a change pass.

## When in doubt

- The .NET API is the **spec** the TS port follows. If a change here would force a divergence in TypeScript, surface it explicitly.
- `README.md` and `docs/` are the authoritative user-facing description of what the library does. Keep them in sync with public-API changes.
