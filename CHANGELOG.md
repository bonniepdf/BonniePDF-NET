# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

## Unreleased

### Features

- **Charting engine:** sync updated BonnieCharts sources across 40 chart families, presentation themes, paint gradients, visual effects, annotations, pseudo-3D depth, and shared chart type fixtures.

### Changes

- **Factur-X:** `FacturX.Apply` now checks the invoice XML before binding it. It must be well-formed UTF-8 CII with no DOCTYPE, and its profile identifier (BT-24) must be a recognised Factur-X / ZUGFeRD 2.1+ profile that matches `profile`. On failure it throws a `FacturXException` carrying a `Code` and leaves the document unchanged. `profile` is now `FacturXProfile?` and defaults to `null`, which reads the profile from the XML instead of defaulting to EN 16931. This is not business-rule validation: totals, VAT rules and the CII schema are still not checked.

### Fixes

- **Compliance preflight and layout snapshots:** every chart is now a figure, so preflight reports `figure-missing-alt` for every chart type. The 26 chart types added after the original 14, such as `FunnelChart`, `GanttChart`, `TreemapChart` and `SankeyChart`, were skipped. They are not Document Definition types yet.

## 2026.11.23

Status: Released
Date: 2026-11-23

### Highlights

- Expand built-in vector charting to 14 chart families with stacked bar, stacked area, dual-axis combo, waterfall, scatter and bubble, bullet, and heatmap charts.
- Add automatic chart axis scaling and layout, compact `K`/`M`/`B`/`T` number formatting, and `modern`, `dark`, and monochrome hatch-pattern `print` themes.

### Features

- **New chart families:** added `StackedBarChart` (`absolute` and 100% `percent` modes), `StackedAreaChart`, dual-axis `ComboChart`, `WaterfallChart`, `ScatterChart` (with bubble sizing and OLS linear trendlines), `BulletChart`, and `HeatmapChart`, with full Document Definition 1.0, PDF/UA tagging, and layout snapshot support.
- **Chart rendering, themes & layout:** added `dark` and monochrome `print` (pure-vector hatch patterns) themes, composable `ChartThemeDefinition` (`CustomTheme`), `AutoScale` (Heckbert nice ticks), `AutoLayout` dynamic margins, `K<n>` compact number formatting (`K`/`M`/`B`/`T`), smooth monotone splines and step curves, dashed series, shaped/hollow/haloed markers, vertical `AreaGradient` fades, `PieChart` radial-elbow callout labels and configurable `DonutHoleRatio`/`SliceBorderWidth`, annotated `ChartReference` labels, card borders, `Subtitle`/`Caption`, flexible `LegendPosition`, and `InheritDocumentFont`.

### Fixes

- **PDF/A, XMP & tagging:** emit UTC `Z` dates and split `,`/`;` keywords in XMP, align ICC profile and tagged `/Link` key order, and emit `/DisplayDocTitle true` without mutating `Settings.DisplayDocTitle`.
- **Text `FontFeatures`:** track explicit assignment so inherited `DefaultTextStyle.FontFeatures` updates on regeneration.
- **Document Definition:** skip unknown element types and enum tokens under `BestEffort = true`, make `PageNumberOf.text` optional, support `SectionLink.Underline` and `Grid.AlternateRowBackgroundColor`, and let per-side border overrides win over `width`.
- **SVG export:** emit `font-display:block` on `@font-face` rules and scope embedded font subsets to `MaxPages`.
- **Layout snapshots & preflight:** write `paint.textBackdrop` in `ToJson()`, capture `VisualToc` paint, record cell fills on split table rows, and update the `table-no-header` fix hint (`AddHeader`, `IsHeader`, `Grid`, `Presentation = true`).
- **Imported SVG fonts:** check `FontManager` before standard aliases in `FontFamilyResolver.ResolveKnown`.

## 2026.11.21

Status: Released
Date: 2026-11-21

### Highlights

- Generate tagged PDF 2.0 documents with PDF/UA-2.
- Combine PDF/UA-2 and PDF/A-4f to retain attachments in an accessible archival PDF.

### Breaking changes

- **Chart references:** `CartesianChartBase.References` is now `IList<ChartReference>` rather than `List<ChartReference>`. Collection initialisers, `Add` and indexing work as before; calls to `List`-only members such as `AddRange` need to go through a `List<ChartReference>` you assign.
- **Render-time validation:** an invalid `Line.DashPattern` or `Table.RunningTotalColumn` now throws `InvalidOperationException` instead of `ArgumentOutOfRangeException`, because the value is checked when the document is generated rather than when it is set.

### Fixes

- **Definition export:** `ToDefinition()` no longer throws `NullReferenceException` for a chart series whose `Style` is `null`; the series is written without a `style` entry.

### Document Engine & API

- **PDF/UA-2:** `AccessibilityMode.PdfUa2` writes PDF/UA-2 (ISO 14289-2:2024) on PDF 2.0, alone or with `PdfAConformance.PdfA4`, and passes veraPDF's PDF/UA-2 profile. On top of PDF/UA-1's tagging it writes the structure tree in the PDF 2.0 namespace, gives every list its numbering style, links each table-of-contents entry to its heading, and makes in-document links, bookmarks and the opening view go to structure elements as well as pages, so assistive technology lands on the right heading. It raises the default PDF version to 2.0; an explicit PDF 1.7, or PDF/A-1, -2 or -3, throws. PDF/UA-1 output is unchanged.
- **Bookmarks on PDF 2.0:** a PDF/A-4 document lost its bookmarks, because they were written for PDF 1.7 only. They are now written on PDF 2.0 too.
- **PDF/A-4 drops attachments:** a PDF/A-4 document with attachments embedded them and claimed PDF/A-4, which veraPDF rejects: PDF/A-4 accepts only embedded files that are themselves PDF/A, and the engine cannot check that. As under PDF/A-1 and -2, the attachments are now dropped with an `Attachment` warning and the claim kept. Use PDF/A-4f to keep them, or PDF/A-3 for e-invoices.
- **PDF/A-4f:** `PdfAConformance.PdfA4f` writes PDF/A-4f (ISO 19005-4, conformance F), the PDF/A-4 level that carries attachments of any type, and passes veraPDF's PDF/A-4f profile. It is PDF 2.0, so it also combines with PDF/UA-2, the only way to have an accessible, archival PDF 2.0 file with attachments. A PDF/A-4f document must have at least one attachment, and throws without one. Factur-X and ZUGFeRD still require PDF/A-3.
- **Attachments state a type under PDF/A:** an attachment with no media type was written without one, which PDF/A-3 and PDF/A-4 forbid, so the document failed its claim. Under any PDF/A level it is now written as `application/octet-stream`. Outside PDF/A nothing changes.
- **Attachments are described under PDF/UA-2:** PDF/UA-2 requires a description on every attached file, and one without a description failed the claim. It is now described by its file name, and each attachment described this way records a `DiagnosticCategory.Attachment` warning whose `Source` is the file name. Set `FileAttachment.Description` for one that tells readers what the file is.

### Packaging & Documentation

- Point `PackageProjectUrl` and README links directly at canonical `https://www.bonniepdf.dev` URLs and expand NuGet package tags and description for PDF/UA, PDF/A, Factur-X, and ZUGFeRD discovery.

### Examples

- Keep the delivery note on one page.

## 2026.11.13

Status: Released
Date: 2026-11-13

### Highlights

- Use the requested page sizes and orientations in the restaurant menu, allergen guide, pallet label, and final inspection record examples.
- Improve text contrast on the orange rating band in the energy performance certificate.

### Examples

- **Restaurant menu, allergen guide, pallet label and final inspection record:** these now use the page sizes and orientations their layouts call for.
- **Energy performance certificate:** text on the orange rating band has higher contrast.
