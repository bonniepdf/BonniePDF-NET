# Document Setup

Configure your document's metadata, page settings, and default styles before adding content.

## Document Metadata

Use `WithMetadata()` to set PDF document properties visible in reader applications:

```csharp
Pdf.Create(doc =>
{
    doc.WithMetadata(metadata =>
    {
        metadata.Title = "Annual Report 2026";
        metadata.Author = "Finance Department";
        metadata.Subject = "Financial performance summary";
        metadata.Creator = "BonniePDF";
    });

    // ...
}).Save("report.pdf");
```

These properties appear in the PDF viewer's Document Properties dialog and are indexed by search engines.

## Document Settings

Use `WithSettings()` to configure page layout, PDF version, and viewer preferences:

```csharp
doc.WithSettings(settings =>
{
    // Page setup
    settings.PageSize = PdfSize.A4;
    settings.Orientation = PageOrientation.Landscape;
    settings.Margin = 60; // points (72 points = 1 inch)

    // PDF version and compliance
    settings.Version = PdfVersion.Pdf17;
    settings.PdfAEnabled = true;

    // Viewer preferences
    settings.DefaultZoom = ZoomLevel.FitPage;
    settings.DisplayDocTitle = true;
    settings.CenterWindow = true;
});
```

### Page Sizes

BonniePDF includes all standard page sizes as static properties on `PdfSize`:

| Size | Dimensions | Common Use |
|---|---|---|
| `PdfSize.A4` | 210 x 297 mm | Default; international standard |
| `PdfSize.Letter` | 8.5 x 11 in | North American standard |
| `PdfSize.Legal` | 8.5 x 14 in | Legal documents |
| `PdfSize.A3` | 297 x 420 mm | Posters, large charts |
| `PdfSize.A5` | 148 x 210 mm | Booklets, flyers |
| `PdfSize.Tabloid` | 11 x 17 in | Large-format printing |
| `PdfSize.Executive` | 7.25 x 10.5 in | Business documents |

**ISO A-series:** `A0`, `A1`, `A2`, `A3`, `A4`, `A5`

**ISO B-series:** `B0`, `B1`, `B2`, `B3`, `B4`, `B5`

**North American:** `Letter`, `Legal`, `Tabloid`, `Ledger`, `Statement`, `Executive`

**Custom sizes:**

```csharp
// From inches
var customSize = PdfSize.FromInches(6, 9, "Paperback");

// From millimeters
var customSize = PdfSize.FromMM(100, 200, "Custom Card");
```

### Page Orientation

```csharp
settings.Orientation = PageOrientation.Portrait;  // default
settings.Orientation = PageOrientation.Landscape;
```

You can also call `.Portrait()` or `.Landscape()` on any `PdfSize`:

```csharp
var landscapeA4 = PdfSize.A4.Landscape();
var portraitLedger = PdfSize.Ledger.Portrait();
```

### PDF Version

| Version | Features |
|---|---|
| `PdfVersion.Pdf14` | Default. RC4 encryption support. |
| `PdfVersion.Pdf17` | AES-256 encryption, PDF/A-1b compliance, XMP metadata, bookmarks. |

### Zoom Level

```csharp
settings.DefaultZoom = ZoomLevel.FitPage;   // Fit entire page (default)
settings.DefaultZoom = ZoomLevel.FitWidth;  // Fit page width
settings.DefaultZoom = ZoomLevel.FitHeight; // Fit page height
settings.DefaultZoom = ZoomLevel.Custom;    // Use custom zoom
settings.CustomZoomLevel = 1.5;             // 150% zoom
```

### Page Mode

Controls how the document appears when first opened:

```csharp
settings.PageMode = PageMode.UseNone;        // Default — clean view
settings.PageMode = PageMode.UseOutlines;    // Show bookmarks panel
settings.PageMode = PageMode.UseThumbs;      // Show thumbnails
settings.PageMode = PageMode.FullScreen;     // Open in full screen
```

### Viewer Preferences

```csharp
settings.HideToolbar = true;      // Hide the toolbar
settings.HideMenubar = true;      // Hide the menu bar
settings.HideWindowUI = true;     // Hide window controls
settings.FitWindow = true;        // Resize window to first page
settings.CenterWindow = true;     // Center on screen
settings.DisplayDocTitle = true;  // Show title instead of filename
```

## Default Text Style

Set a document-wide default for all text elements. Any property not explicitly overridden by an element inherits from this default:

```csharp
doc.WithDefaultTextStyle(style => style
    .WithFamily(StandardFonts.Helvetica)
    .WithSize(11)
    .WithColor(Color.Slate700));
```

This eliminates repetitive `.Font()` and `.FontSize()` calls throughout your document.

### TextStyle Configuration Methods

| Method | Description |
|---|---|
| `.WithFamily(string)` | Base font family name |
| `.WithSize(float)` | Font size in points |
| `.WithColor(Color)` | Text color |
| `.WithWeight(FontWeight)` | Font weight (100–900) |
| `.WithBackground(Color)` | Text background highlight |
| `.Bold()` | Shorthand for `.WithWeight(FontWeight.Bold)` |
| `.Light()` | Shorthand for `.WithWeight(FontWeight.Light)` |
| `.Thin()` | Shorthand for `.WithWeight(FontWeight.Thin)` |
| `.WithItalic()` | Enable italic rendering |
| `.Underline()` | Add underline decoration |
| `.Strikethrough()` | Add strikethrough decoration |

### Style Inheritance

The default text style flows through the document hierarchy:

1. **Document default** — set via `doc.WithDefaultTextStyle()`
2. **Page default** — set via `page.DefaultTextStyle()` (overrides document default)
3. **Element overrides** — set via `.Bold()`, `.FontSize()`, etc. (overrides both)

```csharp
doc.WithDefaultTextStyle(style => style
    .WithFamily(StandardFonts.Helvetica)
    .WithSize(11));

doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // Inherits Helvetica 11pt
        column.Item().Text("Regular body text");

        // Inherits Helvetica, overrides to 24pt Bold
        column.Item().Text("Page Title").Bold().FontSize(24);

        // Inherits Helvetica 11pt, adds italic
        column.Item().Text("Emphasis text").Italic();
    });
});
```

## Adding Pages

Use `doc.Page()` to add a page. Each call creates a new page with its own content, header, and footer slots:

```csharp
doc.Page(page =>
{
    // Optional: page-specific settings
    page.Size(PdfSize.Letter);
    page.Margin(72); // 1 inch
    page.Orientation(PageOrientation.Landscape);

    // Page content
    page.Content().Column(column =>
    {
        column.Item().Text("Page content goes here");
    });
});
```

> **Note:** Most documents use a single `doc.Page()` call. BonniePDF automatically handles pagination — content that exceeds one page flows to subsequent pages. Use multiple `doc.Page()` calls only when you need different page settings (e.g., a landscape chart page followed by portrait text pages).
