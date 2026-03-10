# BonniePDF Examples: Fluent API Migration Guide

This document describes how to convert traditional (imperative object-graph) examples into
the Fluent API style. `MarginExampleFluent.cs` is the canonical reference for the completed
conversion pattern.

---

## Overview

BonniePDF supports two authoring styles:

| Style | Entry point | Orientation |
|---|---|---|
| **Traditional** | `Document doc = new(); ... doc.Save(path)` | Object graph — create elements, set properties, call `doc.Add(...)` |
| **Fluent** | `Pdf.Create(doc => { ... }).Save(path)` | Builder/delegate pattern — configure everything inside nested lambdas |

All converted examples must produce **identical PDF output** to their traditional counterparts.
The fluent version is preferred for new examples because it is more readable and avoids
intermediate variables.

---

## Required Usings

Traditional examples typically use:

```csharp
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;
```

Fluent examples replace element-level usings with:

```csharp
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Extensions;
using BonniePDF.Typography;
using BonniePDF.Examples.Core;
```

Remove any `using BonniePDF.Elements.*` that are no longer needed once elements are expressed
through the fluent builder.

---

## Class & File Conventions

- **File name**: append `Fluent` before `.cs` — e.g. `MarginExample.cs` → `MarginExampleFluent.cs`
- **Class name**: append `Fluent` — e.g. `MarginExample` → `MarginExampleFluent`
- **`Id`**: append `Fluent` to the original — e.g. `"MarginsAndUnits"` → `"MarginsFluent"`
- **`Title`**: append `" (Fluent API)"` — e.g. `"Margins & Units"` → `"Margins (Fluent API)"`
- **`Description`**: update to mention "Fluent API"
- **`FileName`**: append `_fluent` before `.pdf` — e.g. `margin_example.pdf` → `margin_example_fluent.pdf`
- **`Category`**: same as the original
- **Namespace**: same as the original

Add the standard XML doc-comment on the class and the `Generate` method (see
`MarginExampleFluent.cs` for wording).

---

## Core Pattern Mapping

### 1. Document creation & save

**Traditional**
```csharp
Document doc = new();
// ... configure ...
doc.Save(outputPath);
return outputPath;
```

**Fluent**
```csharp
Pdf.Create(doc =>
{
    // ... configure ...
}).Save(outputPath);

return outputPath;
```

When the traditional constructor receives a `DocumentSettings` object:

```csharp
// Traditional
Document doc = new(new DocumentSettings { PageSize = PdfSize.A4, Margin = 60 });

// Fluent — configure inside WithSettings
Pdf.Create(doc =>
{
    doc.WithSettings(settings =>
    {
        settings.PageSize = PdfSize.A4;
        settings.Margin = 60;
    });
}).Save(outputPath);
```

---

### 2. Metadata

**Traditional**
```csharp
doc.Metadata.Title   = "My Document";
doc.Metadata.Author  = "BonniePDF";
doc.Metadata.Subject = "...";
```

**Fluent**
```csharp
doc.WithMetadata(metadata =>
{
    metadata.Title   = "My Document";
    metadata.Author  = "BonniePDF";
    metadata.Subject = "...";
});
```

---

### 3. Settings

**Traditional**
```csharp
doc.Settings.Version    = PdfVersion.Pdf17;
doc.Settings.PdfAEnabled = true;
```

**Fluent**
```csharp
doc.WithSettings(settings =>
{
    settings.Version     = PdfVersion.Pdf17;
    settings.PdfAEnabled = true;
});
```

Watermark (fluent shorthand already works in both styles):
```csharp
// This line is identical in both styles
doc.Settings.WithWatermark("SAMPLE", opacity: 0.12f, rotationDegrees: -40);
```

---

### 4. Page & content layout

Traditional examples call `doc.Add(element)` directly. Fluent examples wrap everything
in a page with a root column:

```csharp
doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // All content goes here as column.Item() calls
    });
});
```

For multi-page documents, call `doc.Page(...)` again for each page (replaces `doc.AddPage()`).

---

### 5. Text blocks

**Traditional**
```csharp
doc.Add(new TextBlock
{
    Text      = "Hello World",
    FontName  = StandardFonts.HelveticaBold,
    FontSize  = 24,
    TextColor = Color.Slate800,
    Margin    = new Thickness(0, 0, 0, 20)
});
```

**Fluent**
```csharp
column.Item()
    .Text("Hello World")
    .Font(StandardFonts.HelveticaBold)
    .FontSize(24)
    .TextColor(Color.Slate800)
    .Margin(0, 0, 0, 20);
```

`Margin` overloads:
- `.Margin(all)` — uniform
- `.Margin(horizontal, vertical)` — symmetric pairs
- `.Margin(left, top, right, bottom)` — individual sides

Text alignment (replaces `Alignment = TextAlignment.Center`):
```csharp
.AlignCenter()
.AlignRight()
.AlignJustify()
// (left is default — no method needed)
```

---

### 6. Containers (backgrounds, borders, padding)

**Traditional**
```csharp
doc.Add(new Container
{
    Margin          = new Thickness(0, 10, 0, 10),
    Padding         = new Thickness(15),
    BackgroundColor = Color.Blue100,
    Border          = new Border(1),
    BorderColors    = new BorderColors(Color.Blue500),
    Child           = new TextBlock { Text = "...", FontName = ..., FontSize = 11, TextColor = Color.Blue900 }
});
```

**Fluent**
```csharp
column.Item()
    .Margin(0, 10, 0, 10)
    .Padding(15)
    .Background(Color.Blue100)
    .Border(1, Color.Blue500)
    .Text("...")
    .Font(StandardFonts.Helvetica)
    .FontSize(11)
    .TextColor(Color.Blue900);
```

When a container holds a non-text child (e.g., a nested column), call the child builder
instead of `.Text(...)`:

```csharp
column.Item()
    .Margin(20, 10, 20, 10)
    .Column(stack =>
    {
        stack.Spacing(5);
        stack.Item().Background(Color.Green100).Padding(10)
            .Text("Item 1").Font(StandardFonts.Helvetica).FontSize(10).TextColor(Color.Green800);
    });
```

---

### 7. Nested columns (VerticalStack)

**Traditional**
```csharp
VerticalStack stack = new() { Margin = new Thickness(20, 10, 20, 10) };
stack.AddChild(new Container { BackgroundColor = Color.Green100, Padding = new Thickness(10), Child = new TextBlock { ... } });
doc.Add(stack);
```

**Fluent**
```csharp
column.Item()
    .Margin(20, 10, 20, 10)
    .Column(stack =>
    {
        stack.Spacing(5); // replaces per-item top margin for uniform spacing

        stack.Item()
            .Background(Color.Green100)
            .Padding(10)
            .Text("Item 1")
            .Font(StandardFonts.Helvetica)
            .FontSize(10)
            .TextColor(Color.Green800);

        // additional items...
    });
```

Note: when a `VerticalStack` used `Margin` on individual children to create spacing, prefer
`stack.Spacing(N)` in the fluent version for uniform gaps.

---

### 8. Tables

**Traditional**
```csharp
Table table = new(1, 2)
{
    Margin              = new Thickness(10, 0, 10, 0),
    CellPadding         = 8,
    Border              = new Border(1),
    BorderColors        = new BorderColors(Color.Slate300),
    HeaderBackgroundColor = Color.Slate200,
    HeaderTextColor     = Color.Slate800
};
table.AddHeader(
    new TextBlock { Text = "Property",    FontName = StandardFonts.HelveticaBold, FontSize = 11 },
    new TextBlock { Text = "Description", FontName = StandardFonts.HelveticaBold, FontSize = 11 }
);
table.AddRow(
    new TextBlock { Text = "Margin.Top", FontName = StandardFonts.Helvetica, FontSize = 10 },
    new TextBlock { Text = "Space above", FontName = StandardFonts.Helvetica, FontSize = 10 }
);
doc.Add(table);
```

**Fluent**
```csharp
column.Item()
    .Margin(10, 0, 10, 0)
    .Table(table =>
    {
        table.Columns(1, 1);
        table.CellPadding(8);
        table.Border(1, Color.Slate300);
        table.HeaderBackground(Color.Slate200);
        table.HeaderTextColor(Color.Slate800);

        table.Header(row =>
        {
            row.Cell().Text("Property").Font(StandardFonts.HelveticaBold).FontSize(11);
            row.Cell().Text("Description").Font(StandardFonts.HelveticaBold).FontSize(11);
        });

        table.Cell().Text("Margin.Top").Font(StandardFonts.Helvetica).FontSize(10);
        table.Cell().Text("Space above").Font(StandardFonts.Helvetica).FontSize(10);
    });
```

`table.Columns(...)` accepts the same column-definition arguments as the `Table` constructor
(equal integer counts or float width arrays).

---

### 9. Horizontal stacks (Row)

The fluent equivalent of `HorizontalStack` uses `.Row(...)`:

**Traditional**
```csharp
HorizontalStack row = new()
{
    Spacing           = 10,
    WidthDistribution = WidthDistribution.Equal
};
row.AddChild(new TextBlock { Text = "A", ... });
row.AddChild(new TextBlock { Text = "B", ... });
doc.Add(row);
```

**Fluent**
```csharp
column.Item().Row(row =>
{
    row.Spacing(10);

    row.Item().Text("A").Font(StandardFonts.Helvetica).FontSize(12);
    row.Item().Text("B").Font(StandardFonts.Helvetica).FontSize(12);
});
```

For weighted widths (replaces `WidthDistribution.Weighted` + `AddChild(element, weight)`):
```csharp
row.RelativeItem(1).Text("25%").Font(...).FontSize(12);
row.RelativeItem(2).Text("50%").Font(...).FontSize(12);
row.RelativeItem(3).Text("75%").Font(...).FontSize(12);
```

For fixed-width columns:
```csharp
row.ConstantItem(120).Text("Label").Font(...);
```

---

### 10. Images

**Traditional**
```csharp
doc.Add(new Image
{
    FilePath = "path/to/image.png",
    Width    = 200,
    Margin   = new Thickness(0, 0, 0, 10)
});
```

**Fluent**
```csharp
column.Item()
    .Margin(0, 0, 0, 10)
    .Image("path/to/image.png")
    .Width(200);
```

---

### 11. Multi-page documents

**Traditional**
```csharp
// ... add content for page 1 ...
doc.AddPage();
// ... add content for page 2 ...
```

**Fluent**
```csharp
doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // page 1 content
    });
});

doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // page 2 content
    });
});
```

---

### 12. Extension methods that carry over unchanged

Some fluent extension methods are used in both styles (they extend element types, not the
builder). These require `using BonniePDF.Extensions;` and do **not** change during migration:

```csharp
.AlignCenter()   // on TextBlock
.AlignRight()
.AlignJustify()
```

`WatermarkSettings.WithWatermark(...)` on `DocumentSettings` is also unchanged.

---

## Step-by-Step Conversion Checklist

1. **Copy** the original `.cs` file, rename it with the `Fluent` suffix.
2. **Update** class name, `Id`, `Title`, `Description`, `FileName` per the naming conventions.
3. **Replace usings**: remove `BonniePDF.Elements.*`; add `BonniePDF.Fluent`,
   `BonniePDF.Fluent.Extensions`, `BonniePDF.Extensions`.
4. **Wrap** the method body in `Pdf.Create(doc => { ... }).Save(outputPath); return outputPath;`
5. **Move** metadata into `doc.WithMetadata(...)`.
6. **Move** settings into `doc.WithSettings(...)` (or constructor args become `WithSettings` calls).
7. **Wrap** content in `doc.Page(page => { page.Content().Column(column => { ... }); })`.
8. **Convert** each `doc.Add(new TextBlock { ... })` → `column.Item().Text(...).Font(...)...`
9. **Convert** each `doc.Add(new Container { ... })` → `column.Item().Margin(...).Padding(...)...`
10. **Convert** vertical stacks → `column.Item().Column(stack => { ... })`.
11. **Convert** horizontal stacks → `column.Item().Row(row => { ... })`.
12. **Convert** tables → `column.Item().Table(table => { ... })`.
13. **Remove** all intermediate local variables (`Table table = new(...)`, `VerticalStack stack`, etc.).
14. **Add** XML doc-comments on the class and `Generate` method.
15. **Run** the example and verify the PDF is visually identical to the original.

---

## Examples Already Converted

| Original | Fluent Version |
|---|---|
| `MarginExample.cs` | `MarginExampleFluent.cs` |

## Examples Remaining

| File | Category | Notes |
|---|---|---|
| `SimpleDocument.cs` | Basics | Multi-page, SVG export |
| `PageSettings.cs` | Basics | Page size, orientation |
| `HeadersFooters.cs` | Layout | Header/footer regions |
| `HorizontalStacks.cs` | Layout | Row/weighted widths |
| `MultiPageFlow.cs` | Layout | Multi-page |
| `TextBlockOverflow.cs` | Layout | Overflow handling |
| `VerticalStackOverflow.cs` | Layout | Overflow handling |
| `BookPageLayout.cs` | Typography | Multi-column |
| `CustomFonts.cs` | Typography | Font loading |
| `DickensNovel.cs` | Typography | Large text flow |
| `EmojisAndUnicode.cs` | Typography | Unicode |
| `RichTextStyles.cs` | Typography | Mixed styles |
| `TextAlignment.cs` | Typography | Alignment |
| `TextDecorations.cs` | Typography | Underline/strikethrough |
| `Hyperlinks.cs` | Elements | Link elements |
| `Images.cs` | Elements | Image embedding |
| `Lists.cs` | Elements | Ordered/unordered lists |
| `ShapesAndBorders.cs` | Elements | Shapes, rounded corners |
| `ChartShowcase.cs` + components | Charting | Chart elements |
| `WatermarkDraft.cs` | Security | Watermark |
| `WatermarkConfidential.cs` | Security | Watermark |
| `Encryption.cs` + variants | Security | Encryption |
| `BusinessInvoice.cs` | Templates | Complex layout |
| `MovieCatalog.cs` | Templates | Complex layout |
| `UserDirectory.cs` | Templates | Complex layout |
| `AsyncGeneration.cs` | Integration | Async override |
| `WebApiBasic.cs` + variants | Integration | Web API patterns |
