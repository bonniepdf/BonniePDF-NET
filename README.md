# BonniePDF

A highly performant PDF generation library for .NET 8+. Zero dependencies, no native libraries, no GDI+.
## Features

**Document Creation**
- Fluent API for declarative document construction
- Automatic pagination and overflow handling
- Headers, footers, and page numbering
- TrueType font embedding with subsetting

**Visual Elements**
- Built-in charts: bar, line, area, pie, lollipop, dot plot, grouped bar
- Tables with automatic page breaks
- SVG export

**Security & Compliance**
- PDF/A-1b archival compliance
- AES-256 encryption with configurable permissions

**Developer Experience**
- Zero dependencies
- Full async support
- Cross-platform: Windows, Linux, macOS

## Installation
```bash
dotnet add package BonniePDF
```

Requires .NET 8.0 or later.

## Quick Start
```csharp
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;
using BonniePDF.Core;

Pdf.Create(doc =>
{
    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            column.Item()
                .Text("Hello, World!")
                .Bold()
                .FontSize(24)
                .TextColor(Color.Slate800);
        });
    });
}).Save("hello.pdf");
```

## Documentation

- [Getting Started](docs/getting-started.md)
- [Page Layout](docs/layout.md)
- [Text and Typography](docs/typography.md)
- [Tables](docs/tables.md)
- [Charts](docs/charts.md)
- [Images and Shapes](docs/media.md)
- [Headers and Footers](docs/templates.md)
- [Encryption and PDF/A](docs/advanced.md)
- [API Reference](docs/api.md)

## Examples

The `Examples/` directory contains working samples covering common use cases: invoices, reports, books, tables, and charts.  Browse them online at [bonniepdf.dev/examples](https://bonniepdf.dev/examples).

## License

BonniePDF Community License 1.0 or Commercial — see [bonniepdf.dev/pricing](https://bonniepdf.dev/pricing) for details.
