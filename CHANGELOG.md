# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

This file is maintained by hand. Add an entry under the current version with
each change to the library.

Because both implementations conform to one specification, a behavioural
change lands in both and is described in both changelogs. Word the entry for
this language — C# spellings, C# type names — but describe the same
behaviour, so the two files can be read side by side.

## Unreleased

- Lower memory for long documents: each page's content is stored as compact bytes once layout moves past it, and a document without page chrome (headers, footers, watermark, page colour or background) compresses each page then instead of holding it as text until writing. A 1,000,000-line document peaks at about 1.2 GB instead of 1.7 GB. Output is unchanged.
- Less allocation while shaping embedded fonts: a font reuses its per-glyph working records between shaping calls. Shaping a short Latin line allocates about two thirds less, and a 100,000-line document allocates 448 MB instead of 702 MB and takes about 8% less time. Output is unchanged.
- Concurrent documents that use the same font no longer queue behind one shaping lock: each concurrent shaping call borrows its own copy of the font's shaping data (retained up to the processor count). Four 20,000-line documents generated at once take about 2.8 s instead of 4.2 s. Output is unchanged.
- Faster shaping of embedded fonts: each OpenType lookup now skips glyphs it cannot act on, while charging exactly the same work against the shaping limits. Shaping a short Latin line takes about a third less time. Output and limit behaviour are unchanged.
- `Save(Stream)` and `SaveAsync(Stream)` accept a stream that cannot seek, such as an HTTP response body or a pipe, so a service can stream a PDF without buffering it first. The writers buffer their output and flush the destination once at the end instead of before every object. Output is unchanged, including offsets when the destination already holds data.
- Faster text with feature overrides (`FontFeatures`) and right-to-left or mixed-direction text: shaping no longer rebuilds the font's layout program for every line. A 5,000-line document using `tnum` takes about a third less time and allocates about 84% less. Output is unchanged.
- `Document.Fonts.Add` reuses the parse of a byte array it has seen before, and the PDF writer shares it, so a font is no longer parsed twice per document. A service that keeps its font bytes in memory parses each font once: a small two-face document takes about 20% less time and allocates about 75% less. Family resolution is memoised per document.
- Safer concurrent generation: font subset prefixes no longer use a shared, unsynchronised `Random`; a font's shaping data is built once when several documents first use it together; colour-glyph and CFF embedding reads take the font's lock; `FontFamilyResolver.RegisterFamily` may run while other documents render.
- Faster pagination of large single tables: column widths are computed once per render instead of on every page, which scanned every row each time. A 200,000-row table now renders in about 7 s instead of 39 s. Output is unchanged.
- A top-level element that does not fit even on an empty page is still skipped after two fresh pages, and now reports a `Render` warning (source `pagination`) in `Document.Diagnostics` instead of disappearing silently.
- Tagged (PDF/UA) documents: a top-level element that continues onto another page now reopens its marked content before its graphics state on every page, as on its first page, instead of interleaving the two.

- Add inherited content direction through `DocumentSettings.ContentDirection` and per-element overrides. Rows and table columns mirror visually while retaining source order; start/end alignment follows direction and left/right stays physical. Embedded Arabic and Hebrew TextBlock content uses bidi and script shaping, including wrapped paragraph context. Styled spans, font-fallback boundaries, lists and page-number tokens require further RTL integration.

- Support static CFF 1 OpenType (`.otf`) fonts through existing font registration: shaping and measurement, cubic SVG outlines, and PDF embedding with source-preserving text extraction. Complete CFF programs retain original hints and subroutines; CFF subsetting, CFF2, `.otc` and WOFF are unsupported.

- Default `QRCode.QuietZone` to four modules, matching the QR standard and TypeScript implementation. Set it to `0` explicitly for borderless codes.

- Add opt-in colour emoji with `DocumentSettings.EmojiRendering = EmojiRendering.Colour`: COLRv0 vector layers and CBDT PNG artwork in PDF and SVG, preserving original Unicode sequences for extraction. `Outline` remains the default; the existing Emojis & Unicode example stays monochrome.
- Decode indexed PNG transparency, including packed 1/2/4-bit palette indices, partial alpha and omitted opaque entries.

- Reuse bounded validated OpenType lookup headers to reduce repeated shaping work while preserving lazy validation, lookup order and operation limits.

- Avoid allocating empty contextual-substitution match lists while preserving glyph output, checked font reads and shaping operation limits.

- Reduce retained text-layout memory by allocating font-feature storage only when overrides are supplied and omitting zero-valued paragraph metadata caches. Text layout, feature validation and PDF output are preserved.

- Reduce transient allocations while painting text: append PDF operands and Unicode metadata directly, reuse document-owned CIDs for repeated single-character sources, and size glyph-code arrays once. Text operators, character mappings and extraction are preserved.

- Reduce shaping allocations for ordered clusters and independent glyph positioning, and reuse bounded OpenType coverage/class reads. Glyph output and shaping limits are preserved.
- Reuse compression of identical embedded font programs in a process-wide cache capped at 4 MiB and 16 entries. Subsets and text mappings remain document-owned; streams are encrypted separately. `FontCache.Clear()` releases these entries and `FontCache.MaxFiles = 0` disables retention.

- Add opt-in EventSource scopes for synchronous generation profiling, covering measurement, font loading/shaping/embedding, image decoding/resizing/compression, painting and PDF writing. Profiling preserves PDF output and is inactive without a diagnostic listener.

- Faster processing of opaque RGBA images through exact opaque-alpha resampling and guarded SIMD channel separation. Pixel values, transparency and resolution settings are preserved, with a scalar fallback on other hardware.
- Optimise the RGBA separation loop on its first invocation, avoiding per-pixel-group span helper calls while tiered compilation is warming up.

- Embedded Latin text now uses OpenType ligatures, GPOS kerning and accent positioning consistently in measurement, PDF and SVG. `FontFeatures` on text elements and TextStyle allows overrides such as `kern: 0` and `liga: 0`. Improved advances can change line and page breaks. Standard-14 text retains its existing behavior.
- Font subsets retain substituted glyphs; cluster ToUnicode mappings and ActualText preserve ligatures and original accent spellings during extraction. SVG outlines substitutions to prevent viewer reshaping. Fallback prefers a whole combining cluster, and emergency wrapping and ellipsis preserve graphemes.

## 2026.10.0

- Publishing now requires successful cross-engine gallery validation of the source commit. Configure `PARITY_WORKFLOW_REPOSITORY` and `PARITY_WORKFLOW_TOKEN` before publishing.

- Fixed stable SVG identifiers for document-owned fonts, background-paint snapshots, and artifact tagging of repeated table-header fragments. Corrected encryption of PDF text strings, XMP metadata handling and AES-256 permissions so accessible encrypted documents pass independent validation.

- Added native `Flip` and `Translate` wrappers, including fluent `FlipHorizontal`, `FlipVertical`, `Translate`, `TranslateX` and `TranslateY`, composed geometry and JSON definitions.

- Added native element rotation with free angles and constrained quarter turns, including document definitions, transformed previews and hyperlink bounds.

### Performance

- **Images are processed once, not once per document.** Decoding a PNG, reducing it to the `MaxImageDpi` cap and compressing it were repeated for every document that placed the same image, and dominated generation time for documents with a logo or letterhead. The results are now kept in a bounded, least-recently-used cache shared by all documents and threads, and reused while the file is unchanged. Output is unaffected. The cache holds at most `ImagePipeline.CacheMaxBytes` (default 32 MiB, counting decoded pixels and original file bytes); set it to `0` to retain nothing between documents, and call `ImagePipeline.ClearCache()` to release it. A logo-bearing invoice now generates about ten times faster after the first document and allocates about a twentieth of the memory.
- **Parsed fonts are reused.** A font loaded from a file path is read and parsed once and kept (up to `FontCache.MaxFiles`, default 16; `0` turns it off, `FontCache.Clear()` empties it); a font supplied as a byte array is parsed once for as long as the array lives. Previously every document re-read and re-parsed each font it used.
- **Faster, lighter PNG decoding and resampling.** PNG rows are inflated into a pooled buffer and defiltered in place instead of copying every row several times, IDAT chunks are no longer copied, alpha is split with specialised loops, and the resolution-cap resampler uses specialised loops and weights each source row once. The resampled pixels are unchanged.
- **Image compression defaults to a balanced level.** Image streams are now compressed at zlib level 6 rather than 9: about a quarter faster on the first document, for output typically 1-2% larger. `ImagePipeline.CompressionLevel` selects `ImageCompressionLevel.Smallest` (the previous behaviour), `Balanced` or `Fast`. Image streams are written with a standard zlib header and checksum from the runtime, replacing a managed Adler-32 loop.

### Changed

- Made example document builders self-contained by including their own helpers, chart data, palettes, and font paths. Added entry-point guidance while retaining explicit asset inputs and the standalone project runners.

- Centralized PDF dictionary keys in documented, context-specific internal constants. Serialized PDF output is unchanged.

- **Breaking: removed options that had no effect.** `LineChart.LineWidth` and `ShowDataPointMarkers` are replaced by `DefaultLineWidth` and `DefaultShowMarkers`; use `DotPlotChart.DefaultDotRadius` and `LollipopChart.DefaultStemWidth`/`DefaultHeadRadius` instead of `DotRadius`, `StemWidth` and `HeadRadius`. Series styles still override these defaults. Removed `PieChart.TotalLabelFontSize`/`TotalValueFontSize` and SVG export `TextMode`/`IncludeLinks` (including `SvgTextMode`); they never controlled rendering. Remove these keys from document definitions: ordinary validation warns, and strict validation rejects them.
- Nullable annotations now describe existing null handling for keywords, list items, layers, SVG alternative text, password helpers, drawing paths and encryption buffers. Runtime behavior is unchanged. Removed unused locals and fields while retaining font stream reads and layout measurement calls.

- **Breaking: the public API now lives in one `BonniePDF` namespace.** `using BonniePDF;` replaces the former `BonniePDF.Core`, `Configuration`, `Elements.*`, `Typography`, `Charts`, `Charts.Models`, `Extensions`, `Rendering`, `Rendering.Images`, `Rendering.Tagging`, `Definition`, `Compliance`, `Export`, `Licensing`, `Security.*`, `Attachments`, `EInvoice` and `Prototyping` imports. Two namespaces remain by design: `BonniePDF.Fluent` for the fluent API (`Pdf.Create`, its descriptors and its extension methods, previously split across `Fluent`, `Fluent.Descriptors` and `Fluent.Extensions`) and `BonniePDF.Drawing` for the canvas and paint types. `SvgUnsupportedFeatureException` and its `UnsupportedFeature` entries remain under `BonniePDF.Rendering.Svg` while the SVG engine is vendored. To migrate, replace the old `using` lines with `using BonniePDF;` (plus `using BonniePDF.Fluent;` or `using BonniePDF.Drawing;` where you use them) and drop the old namespace segment from any fully-qualified name. Behaviour and generated PDF output are unchanged. The classic (`IElement`) and fluent (`IContentContainer`) extension methods stay in separate namespaces so `Padding`, `Margin`, `Size` and similar do not become ambiguous on `Container`; import `BonniePDF.Fluent` only where you use the fluent API.

- **`Document.ToByteArray()` and `ToByteArrayAsync()` are removed.** They were `[Obsolete]` aliases of `GenerateBytes()` and `GenerateBytesAsync()`; TypeScript removed its counterparts in the same release, so the two implementations agree.
- **The chart examples no longer use a label format outside the number grammar.** `LabelFormat = "'£'#,##0"` throws `FormatException` when the chart is drawn; the examples use `"N0"`, and show the currency on the axis with `Prefix`.

- **One spelling for a text colour, decoration, weight and italic.**
  `TextColor` is `Color` (on `TextElementBase`, `Hyperlink`, `TableCell`, `TableRow`,
  `VisualToc` and `Watermark`), `TextDecoration` is `Decoration`, `Weight` is
  `FontWeight` and `IsItalic` is `Italic` (on `TextElementBase` and `TextStyle`),
  matching `TextStyle`, the naming map and TypeScript. The extension method
  `TextColor(...)` on `TextBlock` and `TableCell` is `Color(...)`, and
  `TableCell.WithTextColor` and `TableRow.WithTextColor` are `WithColor`. Compound
  names (`HeaderTextColor`) are unchanged. There are no aliases for the old names.
- **`CanvasElement.CanvasWidth` and `CanvasHeight` are `DrawWidth` and `DrawHeight`**,
  as in TypeScript and the element catalogue.

- **The assembly exposes its API and not its plumbing.** About 60 types that were
  public by accident are now `internal`: the PDF writers (`PdfWriter`, `AsyncPdfWriter`,
  `FontWriter`, ...), `PdfDocumentSerializer`, the image and TrueType readers, the
  encryption and tagging helpers, the shading and layout-recording types, and the
  licence environment classifiers. Members of public types that only the engine used
  (`RenderContext.Resources`, `RenderContext.Tagging`, the parsed font on
  `DocumentFontFace` and `FallbackFont`, ...) are `internal` too. Code that used one of
  these will stop compiling; everything the documentation and examples use is unchanged.
  A test now fails if an internal type becomes public again.

- **`DocumentDefinition.Validate` refuses `settings` combinations that generation refuses.**
  A definition that asks for PDF/A with encryption, AES-256 on PDF 1.4, an "a" level
  without `accessibility: "pdf/ua-1"`, PDF/UA-1 on PDF 2.0 (which includes PDF/A-4),
  or bookmarks on PDF 1.4 is now reported as `incompatible-settings` when it is read,
  instead of throwing later when the document is generated. The rules are data in the
  format description and both implementations evaluate the same list.

- **Chart text is formatted by one culture-invariant grammar, and now matches
  TypeScript.** Chart labels used the machine's regional settings: the y-axis and the
  pie total changed with them (`4,000` on en-US, `4.000` on de-DE, `4 000` on fr-FR),
  and `C` printed `¤` under the invariant culture. They now use a fixed grammar:
  `G` (the default: the shortest exact decimal, no grouping), `F<n>` (fixed
  decimals), `N<n>` (fixed decimals, grouped in thousands) and `P<n>` (times 100,
  then `%`, with no space), `n` from 0 to 15, `.` for decimals and `,` for thousands
  always. Rounding is half away from zero. **A format outside this grammar, such as
  `C0`, `E2`, `D` or a custom pattern, now throws `FormatException` when the chart is
  drawn**, naming the grammar (`D` already threw, and `C` was culture-dependent);
  currency belongs in the axis `Prefix` and `Suffix`. The axis stays grouped, now
  always with commas. `ChartNumberFormat.Format` and `ChartNumberFormat.IsSupported`
  are public. The TypeScript implementation makes the same change and gains the
  `labelFormat` and `totalValueFormat` support it lacked.

### Fixed

- **The XMP packet header now carries a real byte order mark.** The `begin` attribute uses U+FEFF (`EF BB BF` in UTF-8), matching the TypeScript output, instead of three characters that encoded as six bytes. Both synchronous and asynchronous PDF generation use the corrected header.

- **Value labels on a `GroupedBarChart` sit on their own bars.** Every series' label was drawn at the middle of the category, so the labels of neighbouring bars printed on top of each other. Each label is now centred above its bar, or beside it on a horizontal chart.

- **A heading, figure or table inside a pagination wrapper keeps its structure tag.** `EnsureSpace`, `PreventPageBreak`, `ShowEntire`, `ShowIf`, `ShowOnce`, `SkipOnce`, `StopPaging` and `Unconstrained` rendered their child without opening its semantic role, so a `Heading` wrapped in one dropped out of the structure tree (and out of the bookmarks and contents that read it) in a PDF/UA-1 document. The wrapper now opens the child's role as a stack or `Container` does.

- **`DefinitionException` derives from `BonniePdfException`**, so one `catch` handles
  input the library rejects.
- **`SaveAsync` with an already-cancelled token no longer creates the file.**
- **`ImageFormat` no longer lists `Bmp` and `Gif`.** Neither was ever loaded.

- **AES-256 encryption and bookmarks are accepted on PDF 2.0.** `ValidateSettings`
  required `PdfVersion.Pdf17` exactly, so a PDF/A-4 document with bookmarks, or an
  AES-256 document with `Settings.Version = PdfVersion.Pdf20`, generated in
  TypeScript and threw here. Both features now accept `Pdf17` and `Pdf20`, as the
  specification says ("PDF 1.7 or later").

- **The same document writes its fonts in the same order every time.** Font objects were
  written in the enumeration order of a concurrent dictionary keyed by font name, which follows
  the hashes of the names: different in every process, and, for a font added with `doc.Fonts.Add`,
  different in every document. A document with two or more fonts could therefore produce PDFs whose
  bytes differed although its content did not. Fonts are now written in the order they were first
  used.
- **A PDF/UA-1 claim is no longer made over fonts that were not embedded.**
  PDF/UA-1 requires every font to be embedded, as PDF/A does. When the
  standard-14 fonts cannot be embedded (the bundled Liberation fonts were removed
  and no system font was allowed), the PDF/A claim was already withheld, but the
  `pdfuaid` claim was still written. It is now withheld too. The document is still
  tagged: the structure tree, `/Lang` and `/MarkInfo` are unchanged; only the claim
  goes.
- **A withheld conformance claim is now reported.** Each withheld claim adds one
  `Warning` to `Document.Diagnostics`, in the new `DiagnosticCategory.Compliance`,
  with `Source` set to the claim (`"pdf/a-2a"`, `"pdf/ua-1"`, …) and a message
  naming the fonts that could not be embedded.
- **Generating no longer changes the document's settings.** Withholding the PDF/A
  claim used to set `Settings.PdfAEnabled = false`, which reset `Conformance` to
  `None` for good: generating the same document again, even with the fonts
  available, produced no PDF/A claim. The claims are now decided per generation.
  As a consequence, attachments on such a document are gated by the PDF/A level it
  requested, as in the TypeScript implementation.
- **A PDF/UA-only document now carries an OutputIntent.** The specification writes
  one for colour management whenever PDF/UA-1 is requested without PDF/A, and the
  TypeScript implementation always has; this implementation wrote it only for
  PDF/A. PDF/UA-only output gains an sRGB ICC profile and an `/OutputIntents` entry.

### Added

- **A text block can keep its indentation.** By default a `TextBlock` drops the leading spaces of each line, which flattens source code and any other text whose indentation means something. `PreserveWhitespace = true` keeps every space, including leading and repeated ones, and wraps by code point at the last space that fits (or mid-token when there is none), so a long line never overflows. Tabs are not expanded. `Heading` and `PageNumberOf` inherit it; list items and `RichText` do not take it. It is also a Document Definition key. Example: `new TextBlock { Text = source, FontName = StandardFonts.Courier, PreserveWhitespace = true }`.

- **`SvgPageExportOptions.ShapeRendering`** (`SvgShapeRendering`: `Auto`,
  `OptimizeSpeed`, `CrispEdges`, `GeometricPrecision`) writes the root
  `shape-rendering` attribute of an exported page, as TypeScript's
  `shapeRendering` does. `Auto`, the default, writes nothing.
- **`DefinitionIssue.Since`**, the minor version that introduced an item the reader
  knows but the document's version does not include, when the reader can tell.

- **Documents can be built from JSON.** `Document.FromDefinitionAsync(json, options)` (and
  `FromDefinition`) reads a Document Definition, a versioned, language-neutral JSON
  description of a whole document (settings, metadata, pages, every element including charts,
  fonts and other resources), and returns a `Document`. `DocumentDefinition.Validate(json)`
  checks it first and returns every problem at once, each with a JSON Pointer path and a stable
  code (`unknown-type`, `invalid-value`, `dangling-reference`, …), without constructing
  anything; reading throws a `DefinitionException` carrying the same list. Nothing in a
  definition can run code, open a file or fetch a URL: an image or font is embedded as base64 or
  named by a `ref` that the host resolves through `DefinitionReadOptions.Resources`, and
  encryption passwords are supplied through `Passwords`, never stored. Input size, nesting depth
  and element count are limited, and a document written for a newer minor version than this
  release reads is refused with each unrecognised item listed, unless `BestEffort` is set.
  `DocumentDefinition.SupportedVersions` states the range this release reads (1.0). The
  TypeScript implementation gains `fromDefinition` in step with this, as a separate
  `@bonniepdf/core/definition` entry.

- **Documents can be written as JSON.** `doc.ToDefinition()` returns a `DefinitionWriteResult`
  (`Definition`, a `JsonObject`; `Notes`; and `ToJson()`): the same Document Definition
  `FromDefinitionAsync` reads, so the document you read back renders identically. Values equal to
  their defaults are left out, lengths are points, colours are hex (or palette tokens with
  `WritePaletteTokens`) and every enumeration is its token, so writing what was written changes
  nothing. Images, fonts and attachments become entries of `resources`, named `image-1`, `font-1`,
  … and shared when the bytes are identical; set `ResourceRef` to name a resource your code can
  supply again and the definition carries that `ref` and a `sha256` instead of the bytes. A
  construct a definition cannot express (a function-valued page template, a `ShowIf` predicate, an
  `Svg` generator, a custom element) makes `ToDefinition` throw a `DefinitionException` that lists
  every one; `OnUnrepresentable = DefinitionUnrepresentable.Drop` leaves them out and returns a
  `dropped` note for each. Encryption is written without its passwords, with a `secret-omitted`
  note. A text property the author never set (font, size, colour, line height, alignment, a list's
  bullet colour) is not written, so the document's default text style and an enclosing
  container's alignment still apply after the round trip. `MaxImageDpi = null` (no cap) is
  written as `"unlimited"`, which is also what a definition says to read it back. Attachments in a definition may now carry
  `creationDate` and `modificationDate`. The TypeScript implementation gains
  `Document.toDefinition` in step with this.

- **A document can carry its own fonts.** `doc.Fonts.Add(bytes, family:, weight:,
  italic:)` adds a face to that document, and a document holds any number of
  families with any number of weights and styles each. Family, weight and italic
  are read from the font unless you give them. An element's `FontFamily`, `Weight`
  and `IsItalic` (and a `TextStyle`, the document's `DefaultTextStyle` and the
  family names in an imported SVG) resolve against the document's faces first, then
  the global registrations, then the standard fonts. Style is matched first, then
  weight in the CSS font-matching order, so a family with only regular and bold
  still answers semi-bold and medium. Nothing added to one document is visible to
  another, including one generated at the same time on another thread, and the
  global registries are not touched, so two documents can use different files under
  the same family name. A face is embedded and subset as any other TrueType font.
  When a face lacks a glyph, the document's other faces are tried before the global
  fallback fonts. `face.FontName` selects a face anywhere an element takes a font
  name. `TtfFont` gains `WeightClass` and `SelectionItalic`. The TypeScript
  implementation gains `doc.fonts` in step with this.

- **Elements can be marked with a source id.** `DebugSource.Mark(element, sourceId)`
  attaches an opaque id to an element, or to the `Document`, and returns it
  unchanged; `DebugSource.Of(element)` reads it back. A marked element's id appears
  as `SourceId` on its node in the layout snapshot (and as `sourceId` in the export's
  JSON), so a preview can map a rendered box back to the source that produced it. The
  Document's mark also applies to the snapshot's root and to the page created with
  the document. The first mark wins, an empty id is ignored, and the id never reaches
  the PDF or the SVG: a marked and an unmarked document generate identical bytes.
  `LayoutNode.SourceId` is now settable. The TypeScript implementation already had
  this as `markDebugSource` and `debugSourceOf`.

- **Page templates can be built from elements.** `PageTemplate.FromElements(@default:,
  firstPage:, odd:, even:)` returns a template for `HeaderTemplate`, `FooterTemplate`,
  `PageBackground` or `PageForeground` without writing a lambda. For page *n* it
  returns the first present of `firstPage` (page 1 only), then `odd` or `even` by
  parity, then `default`, and nothing when none applies. At least one member is
  required, or it throws `ArgumentException`. The same element is used on every page
  it covers, so `{{CurrentPage}}` and `{{TotalPages}}` are how one footer shows a
  different number on each. The result is an ordinary `Func<int, IElement?>`, and
  output is byte-identical to the equivalent hand-written one.
  `PageTemplate.TryGetElements(template, out elements)` reads the members back, and
  returns `false` for a lambda you wrote yourself. The TypeScript implementation
  gains `pageTemplate` in step with this.

- **The SVG document export carries a version and its compliance report, and
  writes itself as JSON.** `GenerateSvgDocument()` / `GenerateSvgDocumentAsync()`
  now return `Version` (currently `1`, also `SvgDocumentExport.CurrentVersion`),
  and with `CaptureLayout = true` they also return `Report`: the result of
  `CompliancePreflight.Evaluate(snapshot)` with default options, so a host no
  longer runs the check itself, and the report always comes from the same engine
  as the snapshot. The new `ToJson()` writes the export in the form a browser
  previewer loads directly, for example from an HTTP response: camelCase keys, no
  `null` values, severities as `"error"` / `"warning"` / `"info"`, single-precision
  values in their shortest form. It produces the same JSON as the TypeScript
  implementation's `JSON.stringify(result)` for the same document. Like that, it
  leaves `<`, `>` and `&` unescaped to keep SVG markup compact, so it is JSON for a
  response body or a file, not for pasting into an HTML `<script>` element. Without
  `CaptureLayout` neither `Snapshot` nor `Report` is present. An empty
  `Settings.Language` is now recorded in the snapshot as absent rather than as `""`.

- **Images are reduced to the resolution their printed size can use.** An image
  used to be embedded at its source resolution however small it was drawn: a
  2658 x 512 logo placed 175 pt wide was stored at about 1,090 pixels per inch,
  173 KB of a 178 KB invoice. `DocumentSettings.MaxImageDpi` (default 288)
  now caps that. When a PNG carries at least 1.5 times the pixels its drawn size
  needs at the cap, the embedded copy is reduced to `floor(drawn size in points
  x cap / 72)` pixels per axis, never enlarged. The reduction is an exact area
  average in integer arithmetic with premultiplied alpha, so both
  implementations produce identical pixels; it never re-encodes an image
  lossily. The same invoice is 21 KB. An image used more than once is embedded
  once, sized for its largest use. Set `MaxImageDpi` to `null` to embed every image
  as it is, or set `Image.KeepResolution` on an image to exempt that image (one exempt
  placement exempts a shared image everywhere it is used). JPEG, indexed-colour
  and 16-bit images are always embedded as they are, and `Document.Diagnostics` carries an
  `image.resampled` or `image.passed-through` entry for each image the cap
  looked at, so an unexpectedly large file can be traced to its image.

- **The `Svg` element draws an SVG document as vector content.** Paths,
  fills, strokes, gradients, clips and text drawn as glyph outlines go through
  the page's canvas like any other drawing, so the result stays sharp at any
  zoom. Set `SvgData` to markup, `FilePath` to a file, or `SvgGenerator` to a
  function given the box to fill; the fluent API adds `container.Svg("logo.svg")`.
  It sizes itself exactly as `Image` does, with the same `Width`, `Height` and
  `FitMode`, and the document's own `viewBox` and `preserveAspectRatio` place
  its content in that box. Text uses the fonts registered with the document,
  resolved as for a paragraph (`Arial` and `sans-serif` are Helvetica, drawn
  from Liberation Sans), and is never drawn in a font nobody chose. Features
  outside the supported profile - filters, masks, stylesheets and so on - fail
  generation by default with `SvgUnsupportedFeatureException`, naming each one;
  with `StrictUnsupportedFeatures = false` the rest is drawn and each skipped
  feature becomes a diagnostic. Under PDF/UA-1 the element is a figure whose
  alternate text is `AltText`, else the document's own `<title>`.
- **`DocumentSettings.SvgRasterizer`** lets an `Svg` element with
  `StrictUnsupportedFeatures = false` draw what the importer cannot as images
  in their place, using any renderer you supply (SVG markup in, PNG bytes out).
  BonniePDF rasterizes nothing itself. It runs on the async generate paths
  (`GenerateBytesAsync`, `SaveAsync`, and the async SVG export methods, which
  now await it); a synchronous generate whose document would need it fails
  rather than drawing something different.

- **The SVG import engine and its canvas adapter.**
  The engine that parses SVG and draws it as vector content now lives under
  `BonniePDF/Rendering/Svg/`, in the `BonniePDF.Rendering.Svg` namespace. It
  is vendored - maintained upstream and copied here by tooling - so its files
  are never edited in this repository, and a test checks each against the hash
  recorded in `VENDORED.json`. An internal adapter draws what it emits onto any
  `ICanvas`, giving a stroke with no miter limit SVG's default of 4 rather than
  PDF's 10. Its types are internal except `SvgUnsupportedFeatureException` and
  `UnsupportedFeature`, which the `Svg` element above raises.

### Changed

- **The PDF canvas scales a stroke's width and dashes with the transform.**
  Under a canvas transform, path points were transformed but line width and
  dash lengths were not, so a scaled stroke came out too thick or too thin. They
  now scale by the transform's scale factor (exact for uniform scaling and
  rotation). Drawing without a transform is unchanged.
- **The PDF canvas places images under its transform.** `DrawImage` ignored any
  canvas transform; it now draws in the transformed space. Drawing without a
  transform is unchanged.

- **The PDF canvas no longer writes a negative zero.** A gradient fill's matrix
  could print `-0` where the TypeScript implementation prints `0`; both now
  write the same bytes for fills and strokes, pinned by the same tests in both
  suites. Pages look the same.

- **SVG export now agrees with the TypeScript implementation, element for
  element.** The shared specification names SVG as the artefact the conformance
  tooling compares, so four differences between the two exporters were defects
  in that contract rather than formatting preferences:

  - `Color.ToHexString()` returns lower case (`#ff0000`, not `#FF0000`),
    matching TypeScript. This is a public method, so the change is visible to
    callers that compare or display the string.
  - A composited child canvas — a chart, or a `CanvasElement` — is emitted
    inside a `<g transform="translate(…)">` group drawn in its own coordinate
    space, rather than with the offset baked into every coordinate. Nothing
    moves; the structure now matches.
  - A glyph run accumulates its advance cursor in `double` rather than `float`,
    from an origin rounded to the export precision. The single-precision cursor
    compounded its own rounding error along a run, reaching the third decimal on
    a long numeric string.
  - SVG coordinates are formatted the way JavaScript's
    `Number(v.toFixed(precision))` formats them: the exact binary value rounded
    half away from zero. `ToString("F3")` rounds a midpoint to even, and glyph
    positions land on exact midpoints routinely, because a scaled advance is a
    dyadic fraction.

  The fourth, `rx` without `ry` on a rounded rectangle, was already correct here
  and is fixed on the TypeScript side.

- **Text measurement and table column widths are computed in `double`.** Both
  accumulated in `float`, which put a measured string and a divided column width
  a fraction of a thousandth away from the TypeScript implementation's — enough
  to land on the other side of the rounding used for output. Layout still
  exchanges `float`, so no public signature changes.

### Added

- **Pagination control primitives.** `EnsureSpace`, `PreventPageBreak`,
  `ShowEntire`, `StopPaging`, `ShowIf` (with `ShowOnce` and `SkipOnce`) and
  `Unconstrained` are single-child wrappers that decide whether and where their
  child renders, and draw nothing themselves, so they compose with any element
  and with each other. The traditional API takes them as elements; the fluent
  API reaches them through `ContainerPaginationExtensions`, which wraps the rest
  of the chain. TypeScript has the same set.

- **Any measurement can now be written in a unit.** Two spellings, both
  resolving through `Units.ToPoints`, so there is one conversion table and no
  rounding difference between them.

  In an object initializer the unit travels as a call on the number, via the new
  `Points()` / `Inches()` / `Cm()` / `Mm()` / `In(Unit)` extension methods on
  `int`, `float`, and `double`:

  ```csharp
  new Container { Height = 1.Inches(), CornerRadius = 2.Mm(), Padding = new Thickness(10.Mm()) }
  ```

  In a fluent chain it is a second argument — a chain of verbs, so the unit
  rides along with the value:

  ```csharp
  Pdf.Create(doc => doc.Page(page => page.Content()
      .Height(1, Unit.Inch)
      .Padding(10, Unit.Millimeter)));
  ```

  Unit-qualified overloads cover every fluent measurement verb: `Width`,
  `Height`, `Size`, `CornerRadius`, `Padding` (all three arities), the six
  per-side `Padding*` verbs, `Border` and the six per-side `Border*` verbs, and
  the page's `Margin`.

  `Units.Parse` / `Units.TryParse` additionally read a unit-suffixed token
  (`"1in"`, `"10mm"`, `"2.5cm"`, `"72pt"`) for configuration-driven values;
  `Parse` throws on an unrecognised unit rather than resolving to zero. A
  document authored entirely in inches produces byte-identical output to the
  same document in points.

  Dimensionless numbers are deliberately excluded: `LineHeight` is a multiple of
  the font size, `Opacity` a ratio, `RotationDegrees` an angle, and a weighted
  column width a share rather than a distance.

- **`Container.FillHeight`** takes the body content height left on the current
  page instead of sizing to content, and `.FillHeight()` on the fluent surface
  does the same. This is the piece that was missing for the header / body /
  footer page shape: the middle band is whatever the page chrome leaves over,
  which an author cannot compute up front because both templates are functions
  of the page number.

  ```csharp
  new Container { FillHeight = true, VerticalAlignment = VerticalAlignment.End, Child = body }
  ```

  An explicit `Height` wins outright, and fill never *shrinks* content — a
  container whose child outgrows the space left keeps the child's height and
  paginates normally. Measurement is unchanged: it reports the natural content
  height, because a pure measure has no cursor to measure from. Fill re-resolves
  after a page break, against the new page's own reserved band.

- **Licence declaration.** Applications now declare which BonniePDF licence
  applies to them, once at startup:
  `BonniePdfLicense.Tier = LicenseTier.Community;` (or `Evaluation`,
  `Professional`, `Enterprise`), or `BONNIEPDF_LICENSE=community` in the
  environment. An in-code declaration takes precedence.

  It is a **declaration, not an enforcement mechanism**: no licence key, no
  activation, no network call, no telemetry, and no tier-dependent feature
  gating. Every tier includes the complete feature set.

  Generating a document without a declaration throws
  `BonniePdfLicenseException` in an environment positively identified as
  development, test, or CI, and otherwise writes a single warning to `Trace`
  and standard error and continues — a production workload that never declared
  a tier warns rather than failing. `LicenseTier.Evaluation` warns once.
  Declaring any other tier is silent. The notice appears at most once per
  process. `BONNIEPDF_LICENSE_ENFORCEMENT=throw|warn` moves it between fatal
  and non-fatal; there is deliberately no way to switch it off.

  The declared tier **never changes a byte of generated output** — no
  watermark, no metadata field, no free-tier degradation — and the test suite
  asserts byte-identical output across every tier.

- `PackageRequireLicenseAcceptance` on the NuGet package, so the licence is
  presented for explicit acceptance at install time.

- `CompliancePreflight.Evaluate` takes a `CompliancePreflightOptions` overload
  selecting which **rule families** run: `IncludeWcag = false` skips the WCAG 2.2
  contrast rules entirely. The suppressed findings are absent from `Findings`
  *and* from the counts, as if the document had no contrast problems; the
  PDF/UA-1 findings are untouched, and `Passed` cannot change, because no WCAG
  rule emits an error. The single-argument overload runs everything, unchanged.

  The two families answer different questions — PDF/UA-1 findings are conformance
  to a claim the document makes, contrast is an adjacent standard an author may
  be tracking separately — and on a colour-rich document the contrast findings
  can bury the structural ones.

- The accessibility preflight now checks **text colour contrast** against the
  WCAG 2.2 AA thresholds (4.5:1, or 3:1 for text at 18pt and above, or 14pt and
  above when bold). `CompliancePreflight.Evaluate` reports
  `contrast-insufficient` as a warning, with the measured ratio and the
  threshold that applied, so the fix is a colour change rather than an
  investigation.

  Contrast is measured wherever the background can be determined from the
  document model: text inside a `Container` with a fill, a table cell under its
  row/header/footer colour, a `PageColor`, or the default white page. Where it
  cannot — text over layered or freely drawn content, a page painted with a
  background element, or anything partly transparent — the preflight reports
  `contrast-unknown` (info) instead of assuming a colour. Reporting a confident
  wrong verdict would be worse than reporting none.

  These are the first findings that do not cite PDF/UA-1: ISO 14289-1 states no
  numeric contrast requirement, so they carry `Standard = "WCAG 2.2"` and
  `Clause = "WCAG 2.2 SC 1.4.3 (AA)"`. Existing findings are unchanged, and
  because the new findings are warnings and info, `ComplianceReport.Passed`
  keeps its meaning.

  Supporting this, the layout snapshot moves to **schema version 4**: nodes
  carry an optional `NodePaint` (text colour, size, weight, background fill,
  alpha) and `LayoutSnapshot.PageSize` carries the page's resolved fill and
  whether it paints a background element. Capture remains opt-in and
  observational — output is byte-identical with and without it. The new
  `Contrast` class exposes `Ratio`, `RelativeLuminance` and `RequiredRatio` for
  callers that want the maths directly.

### Fixed

- **A `Container`'s horizontal alignment now reaches wrapped text.** `x.Item().AlignCenter().Text("...")`
  centred a one-line title (block positioning of a child narrower than the container) but silently left a
  paragraph long enough to wrap flush left, because there was no narrower child box left to position once
  the text filled the container. A `Container` whose `HorizontalAlignment` is `Start`/`Center`/`End` now
  also establishes that as the **ambient horizontal text alignment** for descendant `TextBlock`/`Heading`/
  `RichText` that set no `Alignment` of their own, through any nesting depth, the way CSS `text-align`
  cascades — an explicit `Alignment` on the text element still always wins, and a nested `Container` can
  set its own ambient for its own subtree. `HorizontalStack.Alignment` is a different, cross-axis concept
  (vertical positioning of row children) and does not feed this. A fluent `.Text(t => ...)` rich-text
  builder that never called `.Align*()` no longer marks the built `RichText`'s alignment as explicit
  either, which had been silently defeating this same fallback for anything routed through it.

- **A table's own border was never drawn.** `table.Border(...)` (`table.border`) applies a border to
  every cell, but a cell's own border is never null, so it always shadowed the table's and the setting did
  nothing. A cell that sets a border uses it; every other cell now takes the table's.
- **A table's cell borders are drawn once per shared edge.** Adjacent cells each drew the edge they share, so
  every internal edge was stroked twice. The earlier cell (left, upper) now draws it, and the later cell draws it
  only when its own border is more prominent (wider, then darker) and so paints over it. A 2 x 2 grid is 12
  strokes rather than 16, and cells with different borders show the more prominent line where they meet.
- **A bordered container around a table is now the table's frame.** The container's border used to be drawn
  before the table and the table inset inside it, so the frame and the outermost cell edges sat side by side as
  a double line. The border is now centred on the table's outer edge, the table is not inset, and the border is
  painted over it, so the frame wins the outer edges. Every other bordered container is unchanged.

- **A table inside padding, a border, a background or a row now splits across pages.**
  Wrapping a tall table, column or paragraph in `Container` styling (`Padding`, `Border`, `Background`) used to turn it into an
  unsplittable box: the whole thing jumped to a fresh page, leaving the page it
  should have started on empty, and a `Row` did the same. A box whose child can split
  now splits with it. Each page carries a fragment of the box, with the top border and
  padding on the first fragment and the bottom border and padding on the last, and its
  background drawn behind that page's content. A `Row` whose items can split lets each
  paginate independently, side by side, until all are done. A box or row that fits, or
  whose items cannot split, is drawn whole exactly as before, and a box with a fixed or
  filling height, or rounded corners, is still unsplittable.

- **A border or background on a table cell's content now spans the whole row.** The
  container a cell's content is wrapped in was drawn at the height of its own text, so
  a short cell beside a tall one, or an empty one, was drawn as a short box and the grid
  came out ragged: the borders and shading of one row had three different heights. The
  container that is the cell's own content now fills the row. Containers nested deeper in
  the cell, and cells styled through the table's own cell properties, are unchanged.

- **Only the last one-sided padding, margin or border call in a chain took effect.**
  `PaddingTop`, `PaddingBottom`, `PaddingLeft`, `PaddingRight`, `PaddingHorizontal`,
  `PaddingVertical`, the `Margin` equivalents, and `BorderTop`, `BorderBottom`,
  `BorderLeft`, `BorderRight`, `BorderHorizontal` and `BorderVertical` each assigned
  a whole value and zeroed the other sides, so `.PaddingVertical(7).PaddingHorizontal(3)`
  kept only the horizontal padding, and `.PaddingLeft(1).PaddingTop(2)` only the top.
  Each now changes only its own side or sides, and each border side keeps its own
  colour, so the calls accumulate. The uniform `Padding(value)`, `Margin(value)` and
  `Border(width)`, and the per-side `Padding(left, top, right, bottom)`, still set every side.

- **`CurrentPageNumber()` and `TotalPages()` printed their placeholders.** In
  `page.Footer().Text(t => { t.CurrentPageNumber(); t.Span(" / "); t.TotalPages(); })`
  the footer read `{{CurrentPage}} / {{TotalPages}}`, because `RichText` did not
  substitute the page tokens that `TextBlock` does. It now does: the current page
  number is filled in as each page is drawn, and the total is a deferred object that
  resolves once the whole document is laid out, exactly as in a `TextBlock`. Each
  token reserves the width of two digits in layout, and alignment corrects for the
  page number's real width. Rich text with no tokens is unchanged.
- **Rich text sized by its container no longer wraps.** A container that gives a text
  its own preferred width as the maximum (a centred footer, an auto-width column)
  could wrap the last word onto a second line: preferred width summed whole span
  widths, while line breaking measures word by word, and the two disagree by
  float rounding and by trailing spaces. Preferred width now comes from the same
  word flow that breaks lines. A centred "Page 1 of 2" footer stays on one line.

- **Styling on a fluent rich-text span was silently dropped.** In
  `container.Text(t => { t.Span("Label: ").Bold(); t.Span("value"); })` the
  `.Bold()`, `.FontColor()`, `.FontFamily()` and every other span modifier
  ran after the span had already been copied, so they changed nothing and the
  text came out as one run in the default style. The same applied to `Line`,
  `CurrentPageNumber` and `TotalPages`. Spans now take their final style. A
  regression from the change that made rich text snapshot span styles.
- **`DefaultTextStyle` did not apply to fluent rich text.** `page.DefaultTextStyle(...)`
  reached `Text("...")` but not `Text(t => ...)`, which kept its own Helvetica 12
  default. Rich text now starts from the page's default style, and a span still
  overrides it field by field.
- **A footer whose content is in a container was not drawn.** `page.Footer().AlignCenter().Text(...)`,
  or any padded or aligned footer, produced nothing: a container checks that it
  fits above the bottom margin, the footer sits inside the bottom margin, so it
  broke the page in the middle of painting the footer and lost the content.
  Header and footer chrome no longer paginates.

- **PNG images with an alpha channel decoded wrongly when their rows used the
  Up, Average or Paeth filter.** The loader reset its record of the previous row
  for every row of an RGBA or grey-plus-alpha PNG, so those filters were applied
  against a row of zeros and the image came out speckled and mis-coloured, with a
  damaged soft mask. Photographs and logos exported by real encoders use these
  filters heavily; the hand-built and filter-None images the tests used did not.
  Alpha PNGs now decode to the original pixels for every filter type, matching
  the TypeScript implementation, which was already correct.

- **A page header or footer now reserves body content space.** Previously the
  header and footer were painted into the margin band while body layout still
  ran from the top margin to the bottom margin, so page chrome taller than the
  margin silently overlapped the content underneath it. The body content band is
  now inset per page by `Max(Margin, headerHeight)` at the top and
  `Max(Margin, footerHeight)` at the bottom, on every page including pages
  created by overflow — the cursor reset that accompanies a page break uses the
  new page's own content top rather than the margin.

  Chrome that fits inside its margin band reserves nothing beyond the margin the
  page already had, so documents whose header and footer are shorter than the
  margin — which is essentially all of them — produce byte-identical output. A
  header or footer that outgrows the margin is also now flush against its own
  page edge instead of being centred in a band it no longer fits, which is what
  stops an oversized footer running off the bottom of the page.

  A header and footer that between them leave no usable body height now throw an
  `InvalidOperationException` naming the page and both measured heights, rather
  than paginating against a content band of zero or negative height.

- The accessibility preflight now detects bold text styled by setting
  `FontName` directly to a bold PostScript name (`"Helvetica-Bold"`,
  `"Times-BoldItalic"`, …), not only via the `Weight` property. That idiom is
  how most examples in this repository author bold text, and the only one
  `Hyperlink` exposes at all — without it, bold-but-large text was held to the
  regular-text 4.5:1 WCAG threshold instead of the 3:1 large-text threshold it
  actually qualifies for. This can only relax an existing finding (a lower
  required ratio, never a higher one), so no prior finding becomes stricter.

- The accessibility preflight now measures contrast on **`Hyperlink`** text.
  Paint capture derived a node's text colour and size only from
  `TextElementBase`, and `Hyperlink` carries its own `TextColor`/`FontSize`
  without deriving from it, so every link reached `CompliancePreflight.Evaluate`
  unannotated and no link could ever be reported as low-contrast - the case
  authors get wrong most often, since link text is routinely recoloured against
  a tinted panel. `Hyperlink` exposes no weight, so it is never reported bold
  and the 4.5:1 body threshold always applies. Nothing else about the element
  changes: capture stays observational, and byte output is untouched.

- A `TableCell` with `ColumnSpan` greater than 1 no longer displaces the cells
  after it. The row loop measured and placed each cell by its **index in the
  row** rather than by the column it actually starts in, so once a cell spanned
  more than one column every later cell in that row was drawn one column short
  of its true position - overlapping the spanned cell - and took its width from
  the wrong column, leaving the row short of the table's full width when the
  weights were uneven. Both the row cursor and the width lookup now advance by
  the number of columns each cell covers. Rows without a `ColumnSpan` are
  unaffected: the starting column equals the cell index, and the arithmetic is
  the same as before.

- A `Table` with fewer declared column weights than a row actually fills no
  longer fails to lay out. Column widths came from the constructor weights
  alone, so a table built as `new Table()` produced an empty width array and
  `MeasureRowHeight` threw `IndexOutOfRangeException` on the first cell; a table
  built as `new Table(3)` with two-cell rows threw the same way on the second.
  The declared weights are now extended with equal weights out to the widest row
  - counting `ColumnSpan`, and across headers, rows and footers alike - so the
  columns a row implies share the width evenly rather than being left without
  one. Tables whose weights already cover every row are unaffected, their widths
  being computed from the same weights as before.

- Page breaks left graphics state unbalanced. The render pipeline wraps each
  element in `SaveState`/`RestoreState`; when an element broke the page from
  inside its own `Render`, the restore ran against the new page and the page the
  element started on kept an unclosed `q` - an invalid content stream per ISO
  32000-1 - and, in SVG export, an unclosed `<g>` that made the page fail to
  parse as XML. `NewPage` now closes open graphics-state scopes on the outgoing
  page and reopens them on the new one, as it already did for marked content.

- SVG page export now positions glyphs the same way as `@bonniepdf/core`, which
  the shared specification pins because SVG is the artefact parity tooling
  compares. Embedded-font text is emitted as one `<text>` per font run carrying
  a per-glyph `x` list, replacing a `<tspan>` per glyph — identical geometry in
  roughly a third of the markup (a representative invoice page went from 50.8 KB
  to 20.7 KB of SVG body).

- `DrawTextRotated` and `DrawTextJustified` now position embedded-font glyphs
  explicitly instead of emitting a plain run and leaving the spacing to the
  viewer. Rotated and justified text in an embedded face could previously land
  at positions the PDF did not use.

- Rotated SVG text turned the opposite way to the PDF it previews. The PDF
  painter rotates counter-clockwise for a positive angle in PDF's y-up space;
  SVG is y-down, so the exporter now negates the angle. Watermarks were the
  visible case.

- `DrawTextRotated` and `DrawTextJustified` hardcoded one branch of the baseline
  calculation instead of calling `BaselineY`, placing text incorrectly when
  `SvgCanvasOptions.PdfBaseline` was not at its default.

- `DrawTextJustified` formatted coordinates and font size with the current
  culture, which emits a comma decimal separator — and so malformed SVG — on
  comma-decimal locales. It now formats invariantly like every other path.

- Embedded families are now bound through a `style` attribute with a generic
  fallback rather than a bare `font-family` presentation attribute, so a host
  stylesheet's `* { font-family: inherit }` reset cannot override the face and
  text stays broadly correct if the `@font-face` fails to load.

- Consolidated duplicated internal logic with no change in behaviour:
  letter/word spacing advance is now calculated in one place shared by the
  text elements and both canvases, the nested-list measure and render loops
  moved to a common base, and `GroupedBarChartElement` derives from
  `ChartElement` rather than repeating it. The SVG painter previously skipped
  a wider set of control characters than the PDF painter when measuring
  spacing; both now use the same rule, so SVG and PDF output agree.

## 2026.7.2

- Baseline at the introduction of this changelog. Earlier history is in the
  repository's commit log.
