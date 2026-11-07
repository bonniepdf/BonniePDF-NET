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

- **Links inside a sentence, and underlined links:** `RichText` gains `link(text, uri, style?)` (`Link` in .NET), backed by a new `TextSpan.link`. The span wraps with the paragraph like any other text; each line it lands on becomes a clickable region described by the whole span text and, in tagged documents, a Link inside the paragraph. The span's style is the link's look. `Hyperlink` gains `underline` (default `false`), drawn at the position and weight of an underline decoration. Both round-trip through document definitions.

- **Donut totals in the centre:** `PieChart` gains `totalPosition` (`TotalPosition` in .NET), `legend` by default. With `center`, a donut draws its total as a large figure with `totalLabel` beneath it in the hole, which widens to fit, instead of as a legend row. A full pie keeps the total in the legend. The option round-trips through document definitions; charts that do not set it are unchanged.

- **Wrapping text no longer fails when its font lacks a character:** a paragraph long enough to wrap, set in an embedded font that lacks a character fallback draws (a full stop or digit in a Hebrew-only face, for example), used to throw `Font has no glyph` from the line breaker. It now lays out and draws the character from the fallback font, as shorter text already did.

- **List markers appear once per item:** a list item that wraps onto more lines now shows its bullet or number beside the first line only. Untagged documents used to repeat the marker on every wrapped line, so one long item read as several; tagged (PDF/UA) documents already drew it once. Items that fit on one line are unchanged.

- **Accented and typographic characters show in standard fonts:** the font dictionary of a non-embedded Helvetica, Times or Courier face now declares `/Encoding /WinAnsiEncoding`. The text was already written as WinAnsi bytes, but without the entry viewers decoded it with the font's built-in StandardEncoding, so ’ “ ” – — … € ™ disappeared and é showed as Ø. Documents that claim PDF/A or PDF/UA, whose standard faces are embedded, were not affected. Symbol and ZapfDingbats are unchanged.

- **Links no longer draw a box:** link annotations now write `/Border [0 0 0]`. Without it a viewer applies the PDF default, a one-point border, and many draw it as a rectangle around every hyperlink, table-of-contents entry and cross-reference. The link text, its colour and the active region are unchanged; no other output changes.

- Added opt-in `Theme = "modern"`, measured label spacing and `BarStyle` rounded value ends/opaque linear gradients on bar and grouped-bar charts. SVG and native PDF share resolved vector geometry; modern SVG text uses PDF baselines. Styles round-trip through document definitions. Legacy output is unchanged.

- **Gradient pie slices:** `PieChart` gains `SliceGradient` (default `false`). Each slice fades from a lighter tint of its colour at the centre, or the donut hole, to its own colour at the rim, drawn as a native PDF shading and an SVG linear gradient. Slice colours must be opaque. The option round-trips through document definitions; pies without it are unchanged.

- **Charts honour `AxisOptions.MinValue`:** Cartesian charts now place values linearly between `MinValue` and `MaxValue`, so marks line up with the axis tick labels; before, a non-zero `MinValue` moved the tick labels but not the data. Bars, grouped bars, area fills and lollipop stems start at zero limited to the axis range: a zero line inside the plot when the range crosses zero, the top edge when every value is negative. The modern theme draws its baseline at that zero line in vertical charts. Drawing a chart with series now throws `InvalidOperationException` when `MaxValue` is not greater than `MinValue`, where it used to produce non-finite geometry. Charts with `MinValue = 0` are unchanged.

- **Chart value labels stay outside their marks:** A value below the axis origin (zero, limited to the axis range) is now labelled below its point, or to the left of it and right-aligned in horizontal charts, with the same gap as a label above. It used to be labelled on the far side of its own bar, stem or fill, where it was hidden by the mark. Applies to every cartesian chart in both themes; labels for values at or above the origin are unchanged. The modern theme still omits a moved label that would collide with another.

- Chart drawing sources are shared with the standalone BonnieCharts engine and compiled into this assembly. Existing chart APIs, document layout, accessibility and output are preserved; no runtime dependency is added.

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
