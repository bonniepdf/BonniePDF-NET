# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

## Unreleased

### Breaking changes

- **Chart references:** `CartesianChartBase.References` is now `IList<ChartReference>` rather than `List<ChartReference>`. Collection initialisers, `Add` and indexing work as before; calls to `List`-only members such as `AddRange` need to go through a `List<ChartReference>` you assign.
- **Render-time validation:** an invalid `Line.DashPattern` or `Table.RunningTotalColumn` now throws `InvalidOperationException` instead of `ArgumentOutOfRangeException`, because the value is checked when the document is generated rather than when it is set.

### Fixes

- **Definition export:** `ToDefinition()` no longer throws `NullReferenceException` for a chart series whose `Style` is `null`; the series is written without a `style` entry.

## 2026.11.13

Status: Released
Date: 2026-11-13

### Highlights

- Use the requested page sizes and orientations in the restaurant menu, allergen guide, pallet label, and final inspection record examples.
- Improve text contrast on the orange rating band in the energy performance certificate.

### Examples

- **Restaurant menu, allergen guide, pallet label and final inspection record:** these now use the page sizes and orientations their layouts call for.
- **Energy performance certificate:** text on the orange rating band has higher contrast.
