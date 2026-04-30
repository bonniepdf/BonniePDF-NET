# BonniePDF

A dependency-free PDF creation library for .NET. No dependancies, native binaries or GDI+. Designed for high-performance scenarios and workloads with a simple, intuitive API.

[![NuGet](https://img.shields.io/nuget/v/BonniePDF)](https://www.nuget.org/packages/BonniePDF)
[![NuGet Downloads](https://img.shields.io/nuget/dt/BonniePDF)](https://www.nuget.org/packages/BonniePDF)

## What it does

- Creates PDF documents from C# code using a fluent or traditional API
- Lays out text, tables, images, lists, and containers across pages automatically
- Renders bar, line, area, pie, lollipop, dot plot, and grouped bar charts as embedded vector graphics
- Embeds TrueType fonts with automatic subsetting
- Encrypts documents with AES-256 and configurable permissions
- Produces PDF/A-1b compliant output for archival

## What it doesn't do

- HTML-to-PDF conversion
- Reading or parsing of existing PDFs
- Form filling

## Installation

```bash
dotnet add package BonniePDF
```

Requires .NET 10.0 or later.

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

**Layout:** Columns, rows, tables with repeating headers, ordered/unordered lists, horizontal stacks, containers with padding/margin/borders/rounded corners. Content flows across pages automatically.

**Text:** 14 standard PDF fonts plus TrueType embedding. Rich text with mixed bold/italic/colour/size within a paragraph. Left, centre, right, and justified alignment. Underline and strikethrough decorations.

**Charts:** Bar, grouped bar, line, area, pie, lollipop, and dot plot. Horizontal and vertical orientations. Multi-series with automatic legends. Charts render as PDF vector graphics and can also be exported as standalone SVG.

**Images:** JPEG and PNG embedding with contain, cover, and fill sizing. PNG alpha transparency. QR code generation with configurable error correction.

**Pages:** All standard sizes (A-series, B-series, North American) plus custom. Portrait and landscape. Headers and footers with page numbering. Watermarks.

**Security:** AES-256 encryption. Separate user/owner passwords. Granular permission control (print, copy, modify, annotate). PDF/A-1b archival compliance with XMP metadata and sRGB profiles.

**Runtime:** Zero native dependencies. Runs on Windows, Linux, and macOS. Full async/await support for web server workloads.

## Examples

The [`Examples/`]([Examples/](https://github.com/bonniepdf/BonniePDF/tree/master/.NET/Examples)) directory contains isolated example projects:

| Example | What it demonstrates |
|---|---|
| SimpleDocument | Basic text and multi-page layout |
| BusinessInvoice | Tables, styled containers, payment details |
| ChartShowcase | All chart types with sample data |
| MovieCatalog | Large table with alternating rows and repeating headers |
| BookPageLayout | Typography, justified text, pull quotes |
| RichTextStyles | Mixed formatting within paragraphs |
| Encryption | Password protection and permission control |
| Watermarking | Diagonal watermark text behind content |
| AsyncGeneration | Async save and in-memory generation |
| WebApi | ASP.NET Core integration patterns |

## Documentation

Full documentation is at [bonniepdf.dev](https://www.bonniepdf.dev).

API guide by topic:

- [Getting Started](.NET/docs/fluent-api/01-getting-started.md)
- [Document Setup](.NET/docs/fluent-api/02-document-setup.md)
- [Page Layout](.NET/docs/fluent-api/03-page-layout.md)
- [Text & Typography](.NET/docs/fluent-api/04-text-and-typography.md)
- [Tables](.NET/docs/fluent-api/05-tables.md)
- [Styling](.NET/docs/fluent-api/06-styling.md)
- [Images & Shapes](.NET/docs/fluent-api/07-images-and-shapes.md)
- [Headers & Footers](.NET/docs/fluent-api/08-headers-and-footers.md)
- [Advanced Features](.NET/docs/fluent-api/09-advanced-features.md)
- [Charting](.NET/docs/fluent-api/11-charting.md)
- [API Reference](.NET/docs/fluent-api/10-api-reference.md)

## Licence

BonniePDF uses a hybrid licence model:

- **Community:** Free for individuals, open-source projects, non-profits, and companies with under $50k annual gross revenue. Full features, no restrictions on what you can build.
- **Professional:** For companies over $50k revenue with up to 5 developers. Perpetual for the version purchased.
- **Enterprise:** Unlimited developers. Priority support.

No licence keys, activation servers or runtime checks.

Details at [bonniepdf.dev/license](https://www.bonniepdf.dev/license/).

## Contributing

Bug reports and feature requests via [GitHub Issues](https://github.com/BonniePDF/BonniePDF/issues).  
Pull requests are welcomed however please open an issue first for anything non-trivial so we can discuss the approach.
