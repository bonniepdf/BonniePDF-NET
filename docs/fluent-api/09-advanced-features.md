# Advanced Features

## Watermarks

Add semi-transparent diagonal text that appears behind all content on every page.

### Simple Watermark

```csharp
doc.WithSettings(settings =>
{
    settings.WithWatermark("DRAFT");
});
```

### Custom Watermark

```csharp
doc.WithSettings(settings =>
{
    settings.WithWatermark("CONFIDENTIAL",
        opacity: 0.12f,           // 0.0 (invisible) to 1.0 (opaque)
        rotationDegrees: -40);    // Angle of rotation
});
```

Watermarks use PDF ExtGState for opacity and the `cm` operator for rotation. They are rendered as semi-transparent diagonal text centered on each page.

## Encryption

Protect documents with password-based encryption.

### Owner-Only Encryption

Restrict editing while allowing viewing:

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17; // Required for AES-256
    settings.Encryption = new EncryptionSettings
    {
        OwnerPassword = "admin123"
    };
});
```

### Password-Protected Document

Require a password to open the document:

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17;
    settings.Encryption = new EncryptionSettings
    {
        UserPassword = "reader123",
        OwnerPassword = "admin123"
    };
});
```

### Permission Control

Fine-grained control over what readers can do:

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17;
    settings.Encryption = new EncryptionSettings
    {
        OwnerPassword = "admin123",
        AllowPrinting = true,
        AllowCopying = false,
        AllowModifying = false,
        AllowAnnotations = true
    };
});
```

## Bookmarks

Add a document outline (table of contents) that appears in the PDF reader's sidebar.

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17; // Required for bookmarks
    settings.PageMode = PageMode.UseOutlines; // Show outline panel on open

    settings.Bookmarks.Add(new BookmarkItem("Chapter 1", 0)); // Page index 0
    settings.Bookmarks.Add(new BookmarkItem("Chapter 2", 3)); // Page index 3
    settings.Bookmarks.Add(new BookmarkItem("Appendix", 10)); // Page index 10
});
```

## PDF/A Compliance

Enable PDF/A-1b archival mode for long-term document preservation:

```csharp
doc.WithSettings(settings =>
{
    settings.Version = PdfVersion.Pdf17;
    settings.PdfAEnabled = true;
});
```

PDF/A mode ensures:
- All fonts are embedded
- Color spaces are properly defined
- XMP metadata is included
- No external dependencies

## Multi-Page Flow

BonniePDF automatically handles content that exceeds a single page. No special configuration is needed.

### Text Overflow

Long text blocks are split at word boundaries and continue on the next page:

```csharp
column.Item()
    .Text(veryLongString)
    .FontSize(11);
```

### Table Overflow

Table rows that don't fit flow to the next page. Enable repeating headers to keep column names visible:

```csharp
column.Item().Table(table =>
{
    table.Columns(1f, 2f, 1f);
    table.RepeatHeaders(); // Headers appear on every page

    table.Header(row =>
    {
        row.Cell().Text("ID").Bold();
        row.Cell().Text("Name").Bold();
        row.Cell().Text("Status").Bold();
    });

    // Add hundreds of rows — they flow automatically
    foreach (var item in largeDataSet)
    {
        table.Cell().Text(item.Id.ToString());
        table.Cell().Text(item.Name);
        table.Cell().Text(item.Status);
    }
});
```

### Column Overflow

Items in a column that don't fit on the current page move to the next:

```csharp
column.Spacing(10);

for (int i = 0; i < 200; i++)
{
    column.Item()
        .Padding(10)
        .Background(Color.Slate50)
        .Text($"Card {i + 1}")
        .FontSize(11);
}
```

## Async Generation

Generate PDFs asynchronously for web server scenarios:

```csharp
var builder = Pdf.Create(doc =>
{
    // document setup...
});

// Async save to file
await builder.SaveAsync("output.pdf");

// Async save to stream
await builder.SaveAsync(responseStream);

// Async save with cancellation
await builder.SaveAsync("output.pdf", cancellationToken);
```

## In-Memory Generation

Generate PDFs as byte arrays without writing to disk — ideal for Web API responses:

```csharp
byte[] pdfBytes = Pdf.Create(doc =>
{
    // document setup...
}).GenerateBytes();

// ASP.NET Core example:
return File(pdfBytes, "application/pdf", "report.pdf");
```

## Stream Output

Write directly to a response stream for better memory efficiency:

```csharp
var builder = Pdf.Create(doc =>
{
    // document setup...
});

// Write to any Stream
builder.Save(responseStream);

// Keep stream open for further writing
builder.Save(responseStream, leaveOpen: true);
```

## Reusable Components

Implement `IComponent` to create reusable building blocks:

```csharp
public class InvoiceHeader : IComponent
{
    public string CompanyName { get; set; }
    public string InvoiceNumber { get; set; }
    public DateTime Date { get; set; }

    public void Compose(IContentContainer container)
    {
        container.Row(row =>
        {
            row.Spacing(20);

            row.RelativeItem().Column(left =>
            {
                left.Item().Text(CompanyName).Bold().FontSize(20);
                left.Item().Text("Your trusted partner").TextColor(Color.Slate500);
            });

            row.AutoItem().Column(right =>
            {
                right.Item().Text($"Invoice #{InvoiceNumber}").Bold();
                right.Item().Text(Date.ToString("MMMM d, yyyy")).TextColor(Color.Slate500);
            });
        });
    }
}

// Usage:
column.Item().Component(new InvoiceHeader
{
    CompanyName = "Acme Corp",
    InvoiceNumber = "INV-2026-001",
    Date = DateTime.Today
});

// Or via generic constructor:
column.Item().Component<SimpleFooter>();
```
