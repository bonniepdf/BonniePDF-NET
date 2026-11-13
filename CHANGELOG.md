# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

## 2026.11.12

### Performance Improvements

- **Smaller, faster start-up:** The bundled Liberation fonts are now read from the assembly the first time a document uses them, not all twelve when `FontManager` is first touched. A process whose documents use their own fonts allocates about 15 MB less at start-up, and its peak working set falls by about 13 MB. Output is byte-for-byte unchanged.
- **Cheaper licence check per document:** After the first document, the licence check no longer inspects the environment or the loaded assemblies on every save. It now costs a single flag read, as documented.

### Document Engine & API

- **Corrupt image data is a catchable error:** a PNG whose pixel data is truncated or is not a valid compressed stream now fails with an `ImageDecodeException` with a message, instead of a bare `InvalidDataException` from deep in the decoder.

- **Images have a size limit:** `ImageLoader.MaxPixels` (100 million by default, about 10,000 x 10,000) is checked against the dimensions a PNG or JPEG declares, before any pixel data is inflated, and a PNG's pixel data may not inflate beyond what its header declares. A small file that claims or unpacks to an enormous image is refused with an `ImageDecodeException` where it used to consume gigabytes of memory. A corrupt embedded colour profile, or one over 16 MiB, is dropped and the image uses its device colour space. Raise the limit for large-format print work.

- **Table rows taller than a page split across pages:** a row too tall for any page used to be drawn past the bottom margin, or skipped when repeated headers left too little room. It now continues onto as many pages as it needs. Each cell carries on from where it stopped; content that cannot split, such as an image or a fixed-height box, is drawn once in the first fragment. Header rows repeat, a running total adds the row once, and in tagged documents the row stays one row with one cell per column.

- **Rounded containers split across pages:** a container with `cornerRadius` used to be kept whole, so one taller than a page ran off it. It now splits like any other container, rounding its top corners on the first page and its bottom corners on the last.

- **Content that fits on no page is clipped and reported:** content that cannot split, such as a fixed-height container, an image, a chart or a rotated element, and is taller than the page body is drawn at the top of a page, clipped at the bottom of the body so the footer and margin stay clean, and recorded as an error diagnostic (source `pagination`) naming the page and how much is cut off. Rotated and flipped elements used to throw in this case. Set `Settings.OversizedContent = OversizedContent.Throw` to make generation fail with `OversizedContentException` instead.

- **Content that cannot split moves to the next page:** an image, barcode or other element placed directly in the document that did not fit in the space left on a page was drawn past the bottom margin. It now starts on the next page.

- **Links with characters outside ASCII open the right address:** a link target such as `https://example.com/café` was written byte for byte, which truncated each such character and produced a different URI. Characters outside printable ASCII, and spaces, are now percent-encoded as UTF-8; an ASCII URI is written exactly as before.

- **A tree that contains itself is reported as such:** adding an element to itself, directly or through a descendant, used to overflow the stack and end the process. Generation now throws `ElementCycleException`, naming the element type.

- **Rectangles keep the winding they are given:** `ICanvas.AddRect` wrote an axis-aligned rectangle as PDF's `re` with its corners normalised to a positive width and height, so a rectangle drawn the other way round to cut a hole filled solid under the nonzero rule. It is now written as four segments in the order given, as HTML canvas and SVG define it, and as the TypeScript library writes it. Canvas elements and imported SVG rectangles produce slightly different PDF bytes; they look the same unless a hole was intended, which now appears.
- **SVG text in OpenType (CFF) fonts is drawn:** text in an imported SVG that resolved to a registered `.otf` font with CFF outlines was drawn as nothing, with no diagnostic. Its glyphs are now drawn from their cubic outlines.
- **Text drawn as shapes in SVG export is announced correctly:** colour emoji and text drawn as glyph outlines (CFF fonts, shaped runs) were written as a group with an `aria-label` but no role, which ARIA does not allow, so assistive technology could ignore the label. The group now carries `role="img"`.
- **SVG `<use>` expansion is bounded:** references that expand to more than 50,000 elements in one drawing are reported as an unsupported feature, so a file of a few hundred bytes can no longer expand to hundreds of thousands of shapes. With strict checking, the default, the import fails; otherwise the expansion stops there.

- **Table grid lines no longer disappear beside shaded cells:** each cell used to paint its fill, content and edges before the next cell began, so the next cell's fill, or the next row's, covered half of an edge already drawn. Thin lines beside a shaded header, a banded row or a shaded cell all but vanished. A row now paints all its fills first, then the bottom edges of the row above, then the cells' content, then its own edges, and holds its bottom edges until the next row's fills are down or the table leaves the page. Tables without fills look the same; the order of drawing operations changes in every table with borders.

- **Table footers inside a container:** a table placed inside a container, stack or other element dropped its footer rows, because the footer was drawn only when the document's current page was its last. Footers now follow the table's last body row wherever the table is placed.

- **Mixed font sizes share one baseline:** in rich text, a span smaller than the text beside it used to be drawn from the top of the line, so it sat high, and a subscript (the 2 in H₂O) appeared raised. Every span on a line now sits on the baseline of the line's largest font, a subscript drops below it and a superscript rises above it. Lines in a single font size are unchanged.

- **Right-to-left text keeps its spaces beside fallback fonts:** a right-to-left line whose digits or Latin come from a fallback font (a Hebrew face without digits, for example) is now resolved and ordered as one line. It used to be resolved one font at a time, so the pieces were painted in logical order and the spaces next to a number landed on the wrong side ("12," lost its spaces). The pieces are now painted in visual order in PDF and SVG, and in PDF the line carries one ActualText in reading order. Lines in a single font are unchanged.

- **Carried-forward page subtotals:** `Table` gains `runningTotalColumn` (`RunningTotalColumn` in .NET) and `TableRow` gains `runningValue`. When a table with a running-total column breaks across pages, each page it leaves ends with a "Carried forward" row and the next opens with a "Brought forward" row, both showing the sum of `runningValue` so far, formatted with `runningTotalFormat` (default `N2`) after `runningTotalPrefix`. `carriedForwardLabel` and `broughtForwardLabel` change the wording. The rows are artifacts in tagged documents, so a screen reader does not read the repeated figure. All the options round-trip through document definitions; tables without them are unchanged.

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
