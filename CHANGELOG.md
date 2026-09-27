# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

This file is maintained by hand. Add an entry under **Unreleased** with each
change to the library; on release, rename it to the version being tagged.

Because both implementations conform to one specification, a behavioural
change lands in both and is described in both changelogs. Word the entry for
this language — C# spellings, C# type names — but describe the same
behaviour, so the two files can be read side by side.

## Unreleased

### Added

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
