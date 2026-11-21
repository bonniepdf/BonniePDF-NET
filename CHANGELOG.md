# Changelog

All notable changes to the `BonniePDF` NuGet package are documented here.
Versions use CalVer (`yyyy.m.minor`), decided by the versioning tool, and are
shared with the TypeScript implementation: `BonniePDF` and `@bonniepdf/core`
at the same version are the same library.

## Unreleased

## 2026.11.21

Status: Released
Date: 2026-11-21

### Highlights

- Generate tagged PDF 2.0 documents with PDF/UA-2.
- Combine PDF/UA-2 and PDF/A-4f to retain attachments in an accessible archival PDF.
- Select PDF/A through the conformance setting.

### Breaking changes

- **Chart references:** `CartesianChartBase.References` is now `IList<ChartReference>` rather than `List<ChartReference>`. Collection initialisers, `Add` and indexing work as before; calls to `List`-only members such as `AddRange` need to go through a `List<ChartReference>` you assign.
- **Render-time validation:** an invalid `Line.DashPattern` or `Table.RunningTotalColumn` now throws `InvalidOperationException` instead of `ArgumentOutOfRangeException`, because the value is checked when the document is generated rather than when it is set.

### Fixes

- **Definition export:** `ToDefinition()` no longer throws `NullReferenceException` for a chart series whose `Style` is `null`; the series is written without a `style` entry.

### Document Engine & API

- **`PdfAEnabled` is removed:** `DocumentSettings.PdfAEnabled` is gone. `Conformance` is the only setting that selects PDF/A; replace `PdfAEnabled = true` with `Conformance = PdfAConformance.PdfA2b`, the level it selected. `AsPdfA()` still selects PDF/A-2b.
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
