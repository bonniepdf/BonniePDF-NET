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

### Fixed

- The accessibility preflight now detects bold text styled by setting
  `FontName` directly to a bold PostScript name (`"Helvetica-Bold"`,
  `"Times-BoldItalic"`, …), not only via the `Weight` property. That idiom is
  how most examples in this repository author bold text, and the only one
  `Hyperlink` exposes at all — without it, bold-but-large text was held to the
  regular-text 4.5:1 WCAG threshold instead of the 3:1 large-text threshold it
  actually qualifies for. This can only relax an existing finding (a lower
  required ratio, never a higher one), so no prior finding becomes stricter.

### Added

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
