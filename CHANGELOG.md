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
