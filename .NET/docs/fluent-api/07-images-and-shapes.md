# Images & Shapes

BonniePDF supports embedding raster images, SVG images, horizontal lines, and QR codes.

## Images

### From File Path

```csharp
column.Item()
    .Image("path/to/logo.png");
```

### From Byte Array

Useful for images loaded from databases, APIs, or embedded resources:

```csharp
byte[] imageData = File.ReadAllBytes("photo.jpg");

column.Item()
    .Image(imageData);
```

### Sizing Images

Constrain image dimensions with container styling:

```csharp
// Fixed width (height scales proportionally)
column.Item()
    .Width(200)
    .Image("logo.png");

// Fixed height
column.Item()
    .Height(100)
    .Image("banner.jpg");

// Fixed dimensions
column.Item()
    .Width(150)
    .Height(150)
    .Image("thumbnail.png");
```

### Image Placeholder

When you don't have an actual image, create a colored placeholder:

```csharp
column.Item()
    .Width(200)
    .Height(100)
    .Background(Color.Amber400)
    .Padding(10)
    .Text("Image Placeholder")
    .FontSize(10)
    .TextColor(Color.Amber900)
    .AlignCenter();
```

## Lines

Add horizontal divider lines:

```csharp
// Default 1pt black line
column.Item().Line();

// Custom thickness
column.Item().Line(2);

// Custom thickness and color
column.Item().Line(1, Color.Slate300);
```

### Styled Dividers

Combine with margin for spaced dividers:

```csharp
column.Item()
    .Margin(0, 16, 0, 16)
    .Line(1, Color.Slate200);
```

## QR Codes

Generate QR codes from any string data:

```csharp
// Default 100pt QR code
column.Item().QRCode("https://bonniepdf.com");

// Custom size
column.Item().QRCode("https://bonniepdf.com", size: 150);
```

### QR Code in Layout

```csharp
column.Item().Row(row =>
{
    row.Spacing(20);

    row.RelativeItem().Column(info =>
    {
        info.Item().Text("Scan to visit our website").Bold();
        info.Item().Text("https://bonniepdf.com").TextColor(Color.Blue600);
    });

    row.ConstantItem(100).QRCode("https://bonniepdf.com", 100);
});
```

## Raw Element Escape Hatch

If you need to insert an element created with the traditional API into a fluent layout:

```csharp
var customElement = new Circle
{
    Radius = 30,
    FillColor = Color.Red,
    StrokeColor = Color.DarkGray,
    StrokeWidth = 2
};

column.Item().Element(customElement);
```

This is useful for shapes or elements not yet available as fluent extensions.
