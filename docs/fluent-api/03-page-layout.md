# Page Layout

BonniePDF uses a composable layout system built on three core containers: **Column**, **Row**, and **Table**. These can be freely nested to create any layout structure.

## Column (Vertical Stack)

A column arranges items vertically, top to bottom. This is the most common layout container — almost every page starts with one.

```csharp
page.Content().Column(column =>
{
    column.Item().Text("First item");
    column.Item().Text("Second item");
    column.Item().Text("Third item");
});
```

### Column Spacing

Set consistent vertical spacing between all items:

```csharp
page.Content().Column(column =>
{
    column.Spacing(10); // 10pt gap between each item

    column.Item().Text("Item 1");
    column.Item().Text("Item 2");
    column.Item().Text("Item 3");
});
```

### Nested Columns

Columns can be nested to group related content:

```csharp
column.Item()
    .Margin(0, 0, 0, 15)
    .Column(card =>
    {
        card.Item()
            .Text("Card Title")
            .Bold()
            .FontSize(14);

        card.Item()
            .Text("Card description text goes here.")
            .FontSize(10)
            .TextColor(Color.Slate600);
    });
```

## Row (Horizontal Stack)

A row arranges items horizontally, left to right. Rows use a flex-like model with relative and fixed-width items.

```csharp
column.Item().Row(row =>
{
    row.Spacing(10); // 10pt gap between items

    row.RelativeItem().Text("Left");
    row.RelativeItem().Text("Center");
    row.RelativeItem().Text("Right");
});
```

### Item Types

| Method | Description |
|---|---|
| `row.RelativeItem()` | Flex item, shares available space equally |
| `row.RelativeItem(float weight)` | Flex item with proportional weight |
| `row.ConstantItem(float width)` | Fixed-width item in points |
| `row.AutoItem()` | Sizes to content |

### Proportional Widths

```csharp
column.Item().Row(row =>
{
    row.Spacing(10);

    // 1:2:3 ratio — items get 1/6, 2/6, 3/6 of available space
    row.RelativeItem(1f).Text("Small");
    row.RelativeItem(2f).Text("Medium");
    row.RelativeItem(3f).Text("Large");
});
```

### Mixed Width Types

```csharp
column.Item().Row(row =>
{
    row.Spacing(10);

    // Fixed 100pt sidebar + flexible main content
    row.ConstantItem(100)
        .Background(Color.Slate100)
        .Padding(10)
        .Text("Sidebar");

    row.RelativeItem()
        .Text("Main content fills remaining space");
});
```

### Vertical Alignment

Control how items of different heights align within the row:

```csharp
column.Item()
    .Height(100)
    .Row(row =>
    {
        row.AlignItems(HorizontalAlignment.Center); // Vertically center items
        row.Spacing(10);

        row.RelativeItem().Text("Short");
        row.RelativeItem().Text("Taller\ncontent\nhere");
    });
```

| Alignment | Description |
|---|---|
| `HorizontalAlignment.Start` | Align to top (default) |
| `HorizontalAlignment.Center` | Center vertically |
| `HorizontalAlignment.End` | Align to bottom |
| `HorizontalAlignment.Stretch` | Stretch to fill height |

## Nesting Layouts

The real power comes from combining columns and rows:

```csharp
page.Content().Column(column =>
{
    // Header row
    column.Item().Row(header =>
    {
        header.RelativeItem()
            .Text("Company Name")
            .Bold()
            .FontSize(20);

        header.AutoItem()
            .Text("Invoice #12345")
            .FontSize(12)
            .TextColor(Color.Slate500);
    });

    // Two-column content
    column.Item()
        .Margin(0, 20, 0, 0)
        .Row(body =>
        {
            body.Spacing(20);

            // Left column
            body.RelativeItem().Column(left =>
            {
                left.Item().Text("Bill To").Bold();
                left.Item().Text("John Doe");
                left.Item().Text("123 Main Street");
            });

            // Right column
            body.RelativeItem().Column(right =>
            {
                right.Item().Text("Ship To").Bold();
                right.Item().Text("Jane Smith");
                right.Item().Text("456 Oak Avenue");
            });
        });
});
```

## Styled Containers

Every `column.Item()` or row item returns an `IContentContainer` that supports styling before content:

```csharp
column.Item()
    .Margin(0, 10, 0, 10)     // Outer spacing
    .Padding(15)                // Inner spacing
    .Background(Color.Blue50)   // Background color
    .Border(1, Color.Blue200)   // Border
    .CornerRadius(8)            // Rounded corners
    .Text("Card content")       // Content
    .TextColor(Color.Blue800);
```

The styling methods are applied in the order you write them — each wraps the previous container. See [Styling](06-styling.md) for full details.

## Multi-Page Flow

BonniePDF automatically paginates content that exceeds a single page. All layout containers support this:

- **Columns** — items that don't fit on the current page flow to the next page
- **Text blocks** — long text wraps and continues on subsequent pages
- **Tables** — rows flow across pages, with optional repeating headers

```csharp
doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // This will automatically span multiple pages
        for (int i = 1; i <= 200; i++)
        {
            column.Item()
                .Margin(0, 0, 0, 10)
                .Text($"Item {i}: This content will flow across pages automatically.");
        }
    });
});
```

No special configuration is needed — pagination is built into the layout engine.

## Empty Containers

Use `.Empty()` for spacers, colored blocks, or placeholders:

```csharp
// Colored spacer
column.Item()
    .Height(2)
    .Background(Color.Slate300)
    .Empty();

// Fixed-height placeholder
column.Item()
    .Height(100)
    .Background(Color.Amber100)
    .Border(1, Color.Amber300)
    .Empty();
```

## Components (Reusable Blocks)

For repeated layout patterns, implement the `IComponent` interface:

```csharp
public class InfoCard : IComponent
{
    public string Title { get; set; }
    public string Description { get; set; }

    public void Compose(IContentContainer container)
    {
        container
            .Padding(12)
            .Background(Color.Slate50)
            .Border(1, Color.Slate200)
            .Column(column =>
            {
                column.Item().Text(Title).Bold().FontSize(14);
                column.Item().Text(Description).FontSize(11).TextColor(Color.Slate600);
            });
    }
}

// Usage:
column.Item().Component(new InfoCard
{
    Title = "Revenue",
    Description = "Up 12% this quarter"
});
```
