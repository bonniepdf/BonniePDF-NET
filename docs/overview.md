# BonniePDF

BonniePDF is a high-performance, **zero-dependency** PDF generation engine for .NET. Engineered for speed and compliance, it features an intuitive Fluent API for building complex documents, reports, integrated charting, and native PDF/A support.

## Overview

BonniePDF provides a modern, type-safe API for generating PDF documents in .NET applications. Unlike traditional PDF libraries that require complex object graphs and imperative configuration, BonniePDF's Fluent API lets you describe your document structure declaratively with clean, chainable method calls.

### Key Features

- **Zero Dependencies**: No external PDF libraries or native code required
- **Fluent API**: Intuitive, chainable syntax for building documents
- **Integrated Charting**: Built-in support for bar, line, area, pie, and lollipop charts
- **PDF/A-1b Compliance**: Native support for archival PDF standards
- **AES-256 Encryption**: Secure your documents with strong encryption
- **High Performance**: Optimized for speed with async/await support
- **Flexible Layout**: Columns, rows, tables, and nested structures
- **Rich Typography**: Custom fonts, text styles, and decorations
- **SVG Export**: Export charts and graphics to SVG format
- **Cross-Platform**: Runs on .NET 8.0+ on Windows, Linux, and macOS

## Quick Start

### Installation

```bash
dotnet add package BonniePDF
```

### Your First Document

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

## Architecture

BonniePDF is organized into several key namespaces:

### Core Namespaces

- **`BonniePDF`**: Main entry point with the [`Document`](.NET/BonniePDF/Document.cs) class for traditional API usage
- **`BonniePDF.Fluent`**: Fluent API entry point with [`Pdf.Create()`](.NET/BonniePDF/Fluent/Pdf.cs) for declarative document building
- **`BonniePDF.Fluent.Extensions`**: Extension methods for containers (`.Column()`, `.Row()`, `.Text()`, `.Padding()`, etc.)
- **`BonniePDF.Extensions`**: Extension methods for text blocks (`.Bold()`, `.FontSize()`, `.TextColor()`, etc.)

### Element System

- **`BonniePDF.Elements`**: Core element interfaces ([`IElement`](.NET/BonniePDF/Elements/IElement.cs), [`ElementBase`](.NET/BonniePDF/Elements/ElementBase.cs))
- **`BonniePDF.Elements.Layout`**: Layout containers ([`VerticalStack`](.NET/BonniePDF/Elements/Layout/VerticalStack.cs), [`HorizontalStack`](.NET/BonniePDF/Elements/Layout/HorizontalStack.cs), [`Page`](.NET/BonniePDF/Elements/Layout/Page.cs))
- **`BonniePDF.Elements.Text`**: Text elements ([`TextBlock`](.NET/BonniePDF/Elements/Text/TextBlock.cs), [`RichText`](.NET/BonniePDF/Elements/Text/RichText.cs), [`Heading`](.NET/BonniePDF/Elements/Text/Heading.cs))
- **`BonniePDF.Elements.Tables`**: Table elements ([`Table`](.NET/BonniePDF/Elements/Tables/Table.cs), [`TableRow`](.NET/BonniePDF/Elements/Tables/TableRow.cs), [`TableCell`](.NET/BonniePDF/Elements/Tables/TableCell.cs))
- **`BonniePDF.Elements.Media`**: Media elements ([`Image`](.NET/BonniePDF/Elements/Media/Image.cs))
- **`BonniePDF.Elements.Shapes`**: Shape elements ([`Line`](.NET/BonniePDF/Elements/Shapes/Line.cs), [`Circle`](.NET/BonniePDF/Elements/Shapes/Circle.cs), [`QRCode`](.NET/BonniePDF/Elements/Shapes/QRCode.cs))
- **`BonniePDF.Elements.Lists`**: List elements ([`OrderedList`](.NET/BonniePDF/Elements/Lists/OrderedList.cs), [`UnorderedList`](.NET/BonniePDF/Elements/Lists/UnorderedList.cs))

### Charting System

- **`BonniePDF.Charts`**: Chart types ([`BarChart`](.NET/BonniePDF/Charts/BarChart.cs), [`LineChart`](.NET/BonniePDF/Charts/LineChart.cs), [`AreaChart`](.NET/BonniePDF/Charts/AreaChart.cs), [`PieChart`](.NET/BonniePDF/Charts/PieChart.cs), [`LollipopChart`](.NET/BonniePDF/Charts/LollipopChart.cs), [`DotPlotChart`](.NET/BonniePDF/Charts/DotPlotChart.cs), [`GroupedBarChart`](.NET/BonniePDF/Charts/GroupedBarChart.cs))
- **`BonniePDF.Charts.Models`**: Chart data models ([`ChartData`](.NET/BonniePDF/Charts/Models/ChartData.cs), [`ChartSeries`](.NET/BonniePDF/Charts/Models/ChartSeries.cs), [`ChartDataPoint`](.NET/BonniePDF/Charts/Models/ChartDataPoint.cs), [`AxisOptions`](.NET/BonniePDF/Charts/Models/AxisOptions.cs))

### Drawing System

- **`BonniePDF.Drawing`**: Device-independent drawing interface ([`ICanvas`](.NET/BonniePDF/Drawing/ICanvas.cs))
- **`BonniePDF.Drawing.Pdf`**: PDF-specific drawing implementation ([`PdfDrawing`](.NET/BonniePDF/Drawing/Pdf/PdfDrawing.cs))
- **`BonniePDF.Drawing.Svg`**: SVG export support ([`SvgCanvas`](.NET/BonniePDF/Drawing/SvgCanvas.cs))

### Configuration

- **`BonniePDF.Configuration`**: Document configuration ([`DocumentSettings`](.NET/BonniePDF/Configuration/DocumentSettings.cs), [`DocumentMetadata`](.NET/BonniePDF/Configuration/DocumentMetadata.cs), [`PdfSize`](.NET/BonniePDF/Configuration/PdfSize.cs), [`PdfVersion`](.NET/BonniePDF/Configuration/PdfVersion.cs))

### Security

- **`BonniePDF.Security`**: Encryption and permissions ([`EncryptionSettings`](.NET/BonniePDF/Security/Models/EncryptionSettings.cs), [`PdfPermissions`](.NET/BonniePDF/Security/Permissions/PdfPermissions.cs))

### Typography

- **`BonniePDF.Typography`**: Font handling ([`StandardFonts`](.NET/BonniePDF/Typography/StandardFonts.cs), [`FontWeight`](.NET/BonniePDF/Typography/FontWeight.cs))
- **`BonniePDF.Typography.TrueType`**: TrueType font support ([`TtfFont`](.NET/BonniePDF/Typography/TrueType/TtfFont.cs), [`TtfSubsetter`](.NET/BonniePDF/Typography/TrueType/TtfSubsetter.cs))

### Rendering

- **`BonniePDF.Rendering`**: PDF rendering pipeline ([`PdfRenderPipeline`](.NET/BonniePDF/Rendering/PdfRenderPipeline.cs), [`PdfDocumentSerializer`](.NET/BonniePDF/Rendering/PdfDocumentSerializer.cs))
- **`BonniePDF.Rendering.Writers`**: PDF writers ([`PdfWriter`](.NET/BonniePDF/Rendering/Writers/PdfWriter.cs), [`AsyncPdfWriter`](.NET/BonniePDF/Rendering/Writers/AsyncPdfWriter.cs))

### Core Types

- **`BonniePDF.Core`**: Fundamental types ([`Color`](.NET/BonniePDF/Core/Color.cs), [`Point`](.NET/BonniePDF/Core/Point.cs), [`Size`](.NET/BonniePDF/Core/Size.cs), [`Thickness`](.NET/BonniePDF/Core/Thickness.cs), [`Border`](.NET/BonniePDF/Core/Border.cs))

## Design Principles

### 1. Fluent API First

The Fluent API provides a declarative, method-chaining approach that makes document structure clear and concise:

```csharp
Pdf.Create(doc =>
{
    doc.WithMetadata(metadata =>
    {
        metadata.Title = "Report";
        metadata.Author = "BonniePDF";
    });

    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            column.Item()
                .Padding(20)
                .Background(Color.White)
                .Border(1, Color.Gray300)
                .Text("Styled content")
                .Bold()
                .FontSize(16);
        });
    });
}).Save("output.pdf");
```

### 2. Device-Independent Drawing

The [`ICanvas`](.NET/BonniePDF/Drawing/ICanvas.cs) interface provides a device-independent drawing surface that works with both PDF and SVG backends:

```csharp
public interface ICanvas
{
    float Width { get; }
    float Height { get; }
    
    void FillRect(float x, float y, float width, float height, Color color);
    void DrawLine(float x1, float y1, float x2, float y2, Color color, float lineWidth);
    void DrawText(float x, float y, string text, Color color, float fontSize, string fontFamily, TextAlignment align);
    // ... more methods
}
```

This abstraction allows charts and graphics to be rendered to multiple formats without modification.

### 3. Type Safety

All configuration is strongly typed with compile-time checking:

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17;  // Enum, not string
    settings.PageSize = PdfSize.A4;        // Strongly typed
    settings.PdfAEnabled = true;           // Boolean flag
});
```

### 4. Async-First

All I/O operations support async/await for optimal performance:

```csharp
await Pdf.Create(doc => { ... })
    .SaveAsync("output.pdf");
```

## Documentation

- **[Getting Started](.NET/docs/fluent-api/01-getting-started.md)** — Introduction to the Fluent API
- **[Document Setup](.NET/docs/fluent-api/02-document-setup.md)** — Metadata, settings, and page configuration
- **[Page Layout](.NET/docs/fluent-api/03-page-layout.md)** — Columns, rows, and nested structures
- **[Text & Typography](.NET/docs/fluent-api/04-text-and-typography.md)** — Fonts, weights, and styles
- **[Tables](.NET/docs/fluent-api/05-tables.md)** — Data tables with headers and borders
- **[Styling](.NET/docs/fluent-api/06-styling.md)** — Margins, padding, borders, and backgrounds
- **[Images & Shapes](.NET/docs/fluent-api/07-images-and-shapes.md)** — Embedding images and shapes
- **[Headers & Footers](.NET/docs/fluent-api/08-headers-and-footers.md)** — Page templates with page numbers
- **[Advanced Features](.NET/docs/fluent-api/09-advanced-features.md)** — Watermarks, encryption, and bookmarks
- **[Charting](.NET/docs/fluent-api/11-charting.md)** — Creating data visualizations
- **[API Reference](.NET/docs/fluent-api/10-api-reference.md)** — Complete type and method reference

## Examples

The [`.NET/Examples`](.NET/Examples) directory contains comprehensive examples demonstrating all features:

- **Basics**: Simple documents, page settings, margins
- **Layout**: Horizontal/vertical stacks, multi-page flow, overflow handling
- **Typography**: Custom fonts, rich text styles, decorations
- **Elements**: Images, QR codes, hyperlinks, shapes, lists
- **Tables**: Data tables with headers, borders, and alternating rows
- **Security**: Encryption, permissions, watermarks
- **Templates**: Business invoices, movie catalogs, user directories
- **Charting**: Bar, line, area, pie, lollipop, dot plot, and grouped bar charts

## License

BonniePDF Community License 1.0 — see [LICENSE](LICENSE) for details.

## Website

[https://bonniepdf.dev](https://bonniepdf.dev)
