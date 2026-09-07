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
