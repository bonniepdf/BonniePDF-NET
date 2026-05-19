# Headers & Footers

BonniePDF supports page-level header and footer templates that render automatically on every page. Templates receive the page number, enabling dynamic content like "Page 1 of 5".

## Setting Up Headers and Footers

Headers and footers are configured in `doc.WithSettings()` using template callbacks:

```csharp
Pdf.Create(doc =>
{
    doc.WithSettings(settings =>
    {
        settings.Header((pageNumber, header) =>
        {
            header.Text($"Document Title - Page {pageNumber}")
                .FontSize(10)
                .TextColor(Color.Slate500)
                .AlignCenter();
        });

        settings.Footer((pageNumber, footer) =>
        {
            footer.Text($"Page {pageNumber}")
                .FontSize(10)
                .TextColor(Color.Slate400)
                .AlignCenter();
        });
    });

    doc.Page(page =>
    {
        page.Content().Column(column =>
        {
            // Content that spans multiple pages...
        });
    });
}).Save("output.pdf");
```

## Template Callback Parameters

| Parameter | Type | Description |
|---|---|---|
| `pageNumber` | `int` | 1-based page number |
| `header` / `footer` | `IContentContainer` | Container for the template content |

The container supports the same fluent methods as any other `IContentContainer` — you can use `.Row()`, `.Column()`, `.Text()`, `.Background()`, etc.

## Total Page Count

Use `TextElementBase.TotalPagesToken` to display the total number of pages:

```csharp
using BonniePDF.Elements.Text;

settings.Footer((pageNumber, footer) =>
{
    footer.Text($"Page {pageNumber} of {TextElementBase.TotalPagesToken}")
        .FontSize(10)
        .TextColor(Color.Slate500)
        .AlignRight();
});
```

The `{{TotalPages}}` token is replaced with the actual count after all content is laid out.

## Complex Header Layouts

Headers and footers support the full layout system:

```csharp
settings.Header((pageNumber, header) =>
{
    header.Row(row =>
    {
        row.Spacing(10);

        row.RelativeItem()
            .Text("Company Name")
            .Bold()
            .FontSize(10)
            .TextColor(Color.Slate700);

        row.AutoItem()
            .Text($"Page {pageNumber}")
            .FontSize(9)
            .TextColor(Color.Slate400);
    });
});
```

## Footer with Divider Line

```csharp
settings.Footer((pageNumber, footer) =>
{
    footer.Column(col =>
    {
        col.Item()
            .Height(1)
            .Background(Color.Slate200)
            .Empty();

        col.Item()
            .Margin(0, 5, 0, 0)
            .Row(row =>
            {
                row.RelativeItem()
                    .Text("Confidential")
                    .FontSize(8)
                    .TextColor(Color.Slate400);

                row.AutoItem()
                    .Text($"Page {pageNumber}")
                    .FontSize(8)
                    .TextColor(Color.Slate400);
            });
    });
});
```

## Important Notes

### Font Styling in Templates

Header and footer templates execute during the rendering phase, **outside** the scope of `doc.Page()`. This means they do **not** inherit the document's default text style set via `WithDefaultTextStyle()`.

You should explicitly set font properties on text elements within templates:

```csharp
// Default style — applies to page content only
doc.WithDefaultTextStyle(style => style.WithFamily(StandardFonts.Helvetica).WithSize(11));

doc.WithSettings(settings =>
{
    settings.Header((pageNumber, header) =>
    {
        // Must set font explicitly — default style doesn't apply here
        header.Text($"Header - Page {pageNumber}")
            .Bold()             // .Bold() works (resolves from default Helvetica)
            .FontSize(10)       // Must set size explicitly
            .TextColor(Color.Slate600);
    });
});
```

### Conditional Headers/Footers

The template function returns void, but you can conditionally add content:

```csharp
settings.Header((pageNumber, header) =>
{
    // Skip header on first page
    if (pageNumber > 1)
    {
        header.Text($"Continued - Page {pageNumber}")
            .FontSize(10)
            .TextColor(Color.Slate500);
    }
});
```

### Margin and Template Space

Headers and footers render within the document's margin area. The `settings.Margin` value determines how much space is available. If your header/footer content is too tall, it may overlap with page content.

A margin of 60pt provides comfortable space for a single-line header and footer. Increase to 80–100pt for multi-line templates.
