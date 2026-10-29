# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

## Unreleased

### Performance Improvements

- **Smaller, faster start-up:** The bundled Liberation fonts are now read from the assembly the first time a document uses them, not all twelve when `FontManager` is first touched. A process whose documents use their own fonts allocates about 15 MB less at start-up, and its peak working set falls by about 13 MB. Output is byte-for-byte unchanged.
- **Cheaper licence check per document:** After the first document, the licence check no longer inspects the environment or the loaded assemblies on every save. It now costs a single flag read, as documented.

### Document Engine & API

- **Source layout matches the TypeScript implementation:** `BarcodeSymbology`, `BarcodeContentException` and `QrErrorCorrection` move to their own files, the honest-claim guard becomes an internal `ConformancePlan`, and a container recognises a table frame through an internal `IBorderFrameHost` marker. Public types, namespaces and output are unchanged.

## 2026.10.28

Status: Prerelease

### Highlights

- Faster paragraph wrapping for everyday prose with curly quotes, dashes, and accented letters
- Multi-fold layout speedups on long documents formatted with embedded fonts
- New runnable recipes for page breaks, practical tables, downloads, font fallback, and batch generation
- New industry invoice, long-basket receipt, and customer letter examples

### Performance Improvements

- **Faster wrapping for everyday prose:** Paragraphs containing curly quotes, dashes, accented letters or symbols such as `€` and `™` now wrap from a single shaping pass, as plain ASCII already did. Long documents in embedded fonts, which PDF/A and PDF/UA always use, generate several times faster; a full novel under PDF/A-2a and PDF/UA-1 drops from about 4.5 s to about 1.2 s. Output is byte-for-byte unchanged.

### Examples

- Add page-break, practical-table, download, font-fallback and diagnostics recipes with an ASP.NET Core host and bounded async batch companion.
- Add large-order invoice, long-basket receipt and multilingual customer-letter examples.
- Refresh conditional headers and explicit image assets; remove the paper-size gallery.

## 2026.10.22

Status: Prerelease

### Highlights

- Granular `DefaultTextStyle` cascading for family, weight, italic, and decoration
- Automatic Liberation font fallback for extended Latin, Greek, and Cyrillic scripts
- Strict glyph-coverage verification via `Settings.MissingGlyphs`
- Expanded Community and Commercial licensing for client, desktop, and mobile apps

### Typography & Styling

- **Granular style inheritance:** `DefaultTextStyle` cascades `FontFamily`, `FontWeight`, `Italic`, and `Decoration` independently across document elements.
- **Extended script coverage:** Standard fonts (`Helvetica`, `Times`, `Courier`) automatically fall back to bundled Liberation faces for extended Unicode ranges while deduplicating embedded font subsets.
- **Strict glyph validation:** `Settings.MissingGlyphs = MissingGlyphs.Throw` enables CI and production pipelines to fail fast if a document contains unmapped characters.
- **Chart palette defaults:** Chart-level colour properties (`BarColor`, `LineColor`, `AreaColor`, `DotColor`, `LollipopColor`) cascade automatically to series without explicit overrides.

## 2026.10.15

Status: Prerelease

### Highlights

- High-throughput text shaping, paragraph wrapping, and large-table layout
- Reduced memory footprint with shared font parsing (`FontCache`)
- Right-to-left (`ContentDirection`) layout with Arabic and Hebrew shaping
- OpenType CFF (`.otf`) font support and opt-in colour emoji

### Performance Improvements

- **Accelerated layout engine:** Single-pass paragraph wrapping, optimised OpenType lookup tables, and single-pass table column sizing deliver multi-fold speedups on data-heavy reports.
- **Low-memory streaming:** Compact page buffers, shared style instances, `FontCache` reuse, and non-seekable `Stream` support in `Save` / `SaveAsync` minimise allocations at scale.
- **Concurrent throughput:** Lock-free per-thread font shaping and SIMD-accelerated opaque image resampling scale cleanly across parallel server workloads.

### Features

- **Right-to-left & bidi typography:** Inherited `ContentDirection` (`Ltr` / `Rtl`) with visual mirroring for rows and tables, plus native Arabic and Hebrew script shaping.
- **OpenType CFF & colour emoji:** Full layout, SVG preview, and PDF embedding support for static `.otf` (CFF 1) fonts and opt-in `EmojiRendering.Colour` (COLRv0 and CBDT).
- **Diagnostics & profiling:** Built-in `EventSource` profiling scopes for layout, font, and image pipelines.

## 2026.10.0

Status: Prerelease

### Highlights

- Portable JSON Document Definitions (`FromDefinition` and `ToDefinition`)
- Native vector `Svg` element and automatic `MaxImageDpi` downsampling
- Declarative `Rotate`, `Flip`, `Translate`, and pagination control elements
- WCAG 2.2 AA colour-contrast preflight checks alongside PDF/UA-1 validation

### Document Engine & API

- **JSON Document Definitions:** Build, validate, and serialise complete documents using language-neutral JSON definitions (`Document.FromDefinition`, `DocumentDefinition.Validate`, `doc.ToDefinition()`).
- **Vector SVG & smart image pipeline:** Render crisp vector graphics with the `Svg` element and automatically cap embedded raster resolution via `DocumentSettings.MaxImageDpi` (default 288 DPI) with LRU caching.
- **Layout & transform primitives:** Native `Rotate`, `Flip`, and `Translate` wrappers, `Container.FillHeight`, declarative `PageTemplate.FromElements`, and pagination controls (`EnsureSpace`, `PreventPageBreak`, `ShowEntire`, `ShowIf`).
- **Accessibility & compliance preflight:** `CompliancePreflight.Evaluate` adds automated WCAG 2.2 AA text-contrast verification alongside PDF/A and PDF/UA-1 conformance checks.
- **Ergonomic authoring:** Unified `BonniePDF` root namespace, document-scoped font registration (`doc.Fonts.Add`), and unit-qualified measurements (`Inches()`, `Mm()`, `Cm()`, `Points()`).
