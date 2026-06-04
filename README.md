# BonniePDF for .NET

[![NuGet](https://img.shields.io/nuget/v/BonniePDF)](https://www.nuget.org/packages/BonniePDF)
[![NuGet downloads](https://img.shields.io/nuget/dt/BonniePDF)](https://www.nuget.org/packages/BonniePDF)
[![License: BonniePDF Community](https://img.shields.io/badge/license-BonniePDF%20Community%201.0-blue.svg)](https://www.bonniepdf.dev/license)

[Live examples](https://www.bonniepdf.dev/examples) | [Documentation](https://www.bonniepdf.dev) | [TypeScript package](https://www.npmjs.com/package/@bonniepdf/core)

BonniePDF builds PDF documents from C# as pure managed code: text, tables, charts, images, and accessible tagged output. It lays content out across pages automatically, embeds fonts, draws charts as vector graphics, and produces archival-grade PDF/A files, all without a single native dependency.

BonniePDF creates new PDFs. It does not convert HTML, read or parse existing PDFs, or fill forms.

## Highlights

- Fast and allocation-conscious, built for high-throughput server workloads and fully async/await aware.
- No dependencies. No System.Drawing, no SkiaSharp, no libpdfium, no native interop. Runs the same on Windows, Linux, and macOS.
- Two APIs over one engine: a declarative fluent API and a traditional imperative API that produce byte-identical output. Use whichever suits the task.
- Automatic flow layout. Columns, rows, tables, and lists reflow across pages on their own.
- Vector charts (bar, line, area, pie, and others) embedded directly, or exported to SVG.
- Compliance support: PDF/A archival output (A-1b through A-3a), PDF/UA-1 tagged accessibility, and AES-256 encryption.

## Install

```bash
dotnet add package BonniePDF
```

Targets .NET 8.0 and runs on .NET 8.0 or later.

## Quick start

### Fluent API

```csharp
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using BonniePDF.Core;

Pdf.Create(doc =>
{
    doc.WithDefaultTextStyle(style => style
        .WithFamily(StandardFonts.Helvetica)
        .WithSize(11));

    doc.Page(page =>
    {
        page.Content().Column(col =>
        {
            col.Item()
                .Text("Quarterly Report")
                .Bold()
                .FontSize(24)
                .TextColor(Color.Slate800)
                .Margin(0, 0, 0, 16);

            col.Item().Table(table =>
            {
                table.Columns(2f, 1f, 1f);
                table.HeaderBackground(Color.Slate800);
                table.HeaderTextColor(Color.White);

                table.Header(row =>
                {
                    row.Cell().Text("Region").Bold();
                    row.Cell().Text("Q3").Bold();
                    row.Cell().Text("Q4").Bold();
                });

                table.Cell().Text("North");
                table.Cell().Text("£142,000");
                table.Cell().Text("£187,000");

                table.Cell().Text("South");
                table.Cell().Text("£96,000");
                table.Cell().Text("£118,000");
            });
        });
    });
}).Save("report.pdf");
```

### Traditional API

```csharp
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

Document doc = new();

doc.Metadata.Title = "Quarterly Report";
doc.Settings.Version = PdfVersion.Pdf17;
doc.Settings.PdfAEnabled = true;

doc.Add(new TextBlock
{
    Text = "Quarterly Report",
    FontName = StandardFonts.HelveticaBold,
    FontSize = 24,
    TextColor = Color.Slate800
});

doc.Save("report.pdf");
```

Both APIs produce identical output.

### Async

```csharp
await doc.SaveAsync("output.pdf");
await doc.SaveAsync(stream);
byte[] bytes = Pdf.Create(doc => { /* ... */ }).GenerateBytes();
```

## Features

| Area | Capabilities |
| --- | --- |
| Layout | Columns, rows, tables with repeating headers, ordered and unordered lists, horizontal stacks, containers with padding, margin, borders, and rounded corners, all flowing across pages automatically |
| Text | 14 standard fonts plus TrueType embedding (auto-subset); rich inline mixing of bold, italic, colour, and size; left, centre, right, and justified alignment; underline and strikethrough |
| Charts | Bar, grouped bar, line, area, pie, lollipop, and dot plot; horizontal and vertical; multi-series with automatic legends; rendered to PDF vectors or standalone SVG |
| Images | JPEG and PNG with contain, cover, and fill sizing plus PNG alpha; QR code generation with configurable error correction |
| Pages | All standard sizes (A-series, B-series, North American) plus custom; portrait and landscape; headers, footers, page numbering; watermarks |
| Security | AES-256 encryption; separate user and owner passwords; granular permissions (print, copy, modify, annotate) |
| Archival and accessibility | PDF/A-1b through A-3a with XMP metadata and sRGB profiles; PDF/UA-1 tagged output, validated against veraPDF |
| Runtime | No native dependencies; Windows, Linux, and macOS; full async/await for web-server workloads |

## Examples

The [`Examples/`](Examples/) directory contains isolated, runnable example projects that also serve as a smoke test of the public API:

| Example | Demonstrates |
| --- | --- |
| SimpleDocument | Basic text and multi-page layout |
| BusinessInvoice | Tables, styled containers, payment details |
| ChartShowcase | Every chart type with sample data |
| MovieCatalog | Large table with alternating rows and repeating headers |
| BookPageLayout | Typography, justified text, pull quotes |
| RichTextStyles | Mixed formatting within a paragraph |
| Encryption | Password protection and permission control |
| Watermarking | Diagonal watermark behind content |
| AsyncGeneration | Async save and in-memory generation |
| WebApi | ASP.NET Core integration patterns |

Every example can also be run live in your browser at [bonniepdf.dev/examples](https://www.bonniepdf.dev/examples).

## Documentation

Full guides and the API reference are at [bonniepdf.dev](https://www.bonniepdf.dev), covering document setup, page layout, typography, tables, styling, images, headers and footers, charting, and advanced features.

This is the reference implementation. The [TypeScript port](https://www.npmjs.com/package/@bonniepdf/core) mirrors its API so the two stay consistent across languages.

## Licence

BonniePDF uses a hybrid licence model:

- Community: Free for individuals, open-source projects, non-profits, and companies with under $1m annual gross revenue. Full features, no restrictions on what you can build.
- Commercial: For any form of commercial usage. Perpetual for the version purchased.

No licence keys, activation servers, or runtime checks. Full details at [bonniepdf.dev/license](https://www.bonniepdf.dev/license/).

## Contributing

Bug reports and feature requests go through [GitHub Issues](https://github.com/BonniePDF/BonniePDF/issues). Pull requests are welcome; for anything non-trivial, please open an issue first to discuss the approach.

© 2026 BonniePDF
