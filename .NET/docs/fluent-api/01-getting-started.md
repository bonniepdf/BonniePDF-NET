# Getting Started with the Fluent API

BonniePDF's Fluent API provides a declarative, method-chaining approach to building PDF documents. Instead of creating element objects and setting properties individually, you describe your document structure in a single expression tree.

## Installation

```bash
dotnet add package BonniePDF
```

## Your First Document

```csharp
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
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

This creates a single-page A4 document with a bold heading — in just 14 lines.

## Key Concepts

### 1. Entry Point

Every fluent document starts with `Pdf.Create()`. This returns a `PdfBuilder` that you configure with a lambda, then call `.Save()` or `.GenerateBytes()`.

```csharp
Pdf.Create(doc =>
{
    // configure the document here
}).Save("output.pdf");
```

### 2. Method Chaining

Container styling methods (`.Padding()`, `.Margin()`, `.Background()`, `.Border()`) return `IContentContainer`, allowing you to chain them before adding content:

```csharp
column.Item()
    .Margin(0, 0, 0, 10)    // IContentContainer
    .Padding(15)              // IContentContainer
    .Background(Color.Blue100) // IContentContainer
    .Border(1, Color.Blue500)  // IContentContainer
    .Text("Styled content")   // TextBlock (terminal)
    .FontSize(12)              // TextBlock
    .TextColor(Color.Blue900); // TextBlock
```

### 3. Terminal vs. Continuation Methods

- **Container methods** (`.Padding()`, `.Background()`, etc.) return `IContentContainer` — you keep chaining.
- **Content methods** (`.Text()`, `.Column()`, `.Row()`, `.Table()`) are terminal — they place content into the container.
- `.Text()` returns a `TextBlock`, so you can chain text-specific styling after it.
- `.Column()`, `.Row()`, `.Table()`, `.Empty()` return `void` and end the chain.

### 4. Default Text Style

Set a document-wide default to eliminate repetitive font/size/color calls:

```csharp
Pdf.Create(doc =>
{
    doc.WithDefaultTextStyle(style => style
        .WithFamily(StandardFonts.Helvetica)
        .WithSize(11));

    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            // Inherits Helvetica 11pt — no .Font() or .FontSize() needed
            column.Item().Text("Body text inherits the default.");

            // Override only what differs
            column.Item().Text("Bold heading").Bold().FontSize(20);
        });
    });
}).Save("output.pdf");
```

## Required Namespaces

| Namespace | Purpose |
|---|---|
| `BonniePDF.Fluent` | `Pdf.Create()` entry point and `PdfBuilder` |
| `BonniePDF.Fluent.Extensions` | Container extensions (`.Column()`, `.Row()`, `.Table()`, `.Text()`, `.Padding()`, `.Background()`, etc.) |
| `BonniePDF.Extensions` | TextBlock extensions (`.Bold()`, `.Italic()`, `.FontSize()`, `.TextColor()`, etc.) |
| `BonniePDF.Typography` | `StandardFonts`, `FontWeight` |
| `BonniePDF.Core` | `Color`, `Thickness`, `Border` |
| `BonniePDF.Configuration` | `PdfSize`, `PdfVersion`, `PageOrientation` |

## Saving Options

```csharp
var builder = Pdf.Create(doc => { /* ... */ });

// Save to file
builder.Save("output.pdf");

// Save to stream
builder.Save(stream);

// Generate in-memory byte array
byte[] bytes = builder.GenerateBytes();

// Async variants
await builder.SaveAsync("output.pdf");
await builder.SaveAsync(stream);
```

## Complete Example

```csharp
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;

Pdf.Create(doc =>
{
    doc.WithMetadata(metadata =>
    {
        metadata.Title = "Quarterly Report";
        metadata.Author = "BonniePDF";
    });

    doc.WithSettings(settings =>
    {
        settings.PageSize = PdfSize.A4;
        settings.Version = PdfVersion.Pdf17;
        settings.PdfAEnabled = true;
    });

    doc.WithDefaultTextStyle(style => style
        .WithFamily(StandardFonts.Helvetica)
        .WithSize(11));

    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            column.Item()
                .Text("Quarterly Report")
                .Bold()
                .FontSize(28)
                .TextColor(Color.Slate800)
                .Margin(0, 0, 0, 20);

            column.Item()
                .Text("This report summarizes key metrics for the quarter.")
                .TextColor(Color.Slate600)
                .Margin(0, 0, 0, 16);

            column.Item()
                .Padding(15)
                .Background(Color.Blue50)
                .Border(1, Color.Blue200)
                .Text("Revenue increased 12% quarter-over-quarter.")
                .TextColor(Color.Blue800);
        });
    });
}).Save("report.pdf");
```

## Next Steps

- [Document Setup](02-document-setup.md) — metadata, settings, and page configuration
- [Page Layout](03-page-layout.md) — columns, rows, and nested structures
- [Text & Typography](04-text-and-typography.md) — fonts, weights, styles, and decorations
- [Tables](05-tables.md) — data tables with headers, borders, and alternating rows
- [Styling](06-styling.md) — margins, padding, borders, backgrounds, and sizing
- [Images & Shapes](07-images-and-shapes.md) — embedding images, lines, and QR codes
- [Headers & Footers](08-headers-and-footers.md) — page templates with dynamic page numbers
- [Advanced Features](09-advanced-features.md) — watermarks, encryption, bookmarks, and multi-page flow
- [Charting](11-charting.md) — creating data visualizations with bar, line, area, and pie charts
- [API Reference](10-api-reference.md) — complete type and enum reference
