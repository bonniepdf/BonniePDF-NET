# Styling

Every `IContentContainer` supports a consistent set of styling methods that can be chained before adding content. These methods wrap the container in layers — the order you write them is the order they apply.

## Margin

Margins create space **outside** the element — between it and its siblings.

```csharp
// Uniform margin on all sides
column.Item().Margin(10).Text("10pt margin");

// Horizontal and vertical
column.Item().Margin(20, 10).Text("20pt left/right, 10pt top/bottom");

// Individual sides (left, top, right, bottom)
column.Item().Margin(0, 20, 0, 10).Text("20pt top, 10pt bottom");
```

| Overload | Parameters | Description |
|---|---|---|
| `.Margin(float)` | uniform | All four sides |
| `.Margin(float, float)` | horizontal, vertical | Left/right and top/bottom |
| `.Margin(float, float, float, float)` | left, top, right, bottom | Individual sides |

## Padding

Padding creates space **inside** the element — between its border and its content.

```csharp
// Uniform padding
column.Item().Padding(15).Text("15pt padding");

// Horizontal and vertical
column.Item().Padding(20, 10).Text("20pt left/right, 10pt top/bottom");

// Individual sides (left, top, right, bottom)
column.Item().Padding(10, 20, 10, 5).Text("Custom padding");
```

| Overload | Parameters | Description |
|---|---|---|
| `.Padding(float)` | uniform | All four sides |
| `.Padding(float, float)` | horizontal, vertical | Left/right and top/bottom |
| `.Padding(float, float, float, float)` | left, top, right, bottom | Individual sides |
| `.PaddingTop(float)` | top | Top only |
| `.PaddingBottom(float)` | bottom | Bottom only |
| `.PaddingLeft(float)` | left | Left only |
| `.PaddingRight(float)` | right | Right only |
| `.PaddingHorizontal(float)` | horizontal | Left and right |
| `.PaddingVertical(float)` | vertical | Top and bottom |

## Background

Set the background color of a container:

```csharp
column.Item()
    .Padding(15)
    .Background(Color.Blue100)
    .Text("Blue background")
    .TextColor(Color.Blue900);
```

## Border

Add borders around a container:

```csharp
// Uniform border
column.Item().Border(1, Color.Slate300).Text("Bordered");

// Thick border, default black
column.Item().Border(2).Text("Thick border");

// Individual sides
column.Item()
    .BorderBottom(2, Color.Blue500)
    .Text("Underlined effect");
```

| Method | Description |
|---|---|
| `.Border(float width)` | Uniform border (black) |
| `.Border(float width, Color color)` | Uniform border with color |
| `.BorderTop(float, Color?)` | Top edge only |
| `.BorderBottom(float, Color?)` | Bottom edge only |
| `.BorderLeft(float, Color?)` | Left edge only |
| `.BorderRight(float, Color?)` | Right edge only |

## Corner Radius

Round the corners of a container (requires a background or border to be visible):

```csharp
column.Item()
    .Padding(15)
    .Background(Color.Green100)
    .Border(1, Color.Green400)
    .CornerRadius(8)
    .Text("Rounded card");
```

## Size Constraints

Fix the width or height of a container:

```csharp
// Fixed height
column.Item()
    .Height(100)
    .Background(Color.Amber100)
    .Text("100pt tall");

// Fixed width
column.Item()
    .Width(200)
    .Background(Color.Purple100)
    .Text("200pt wide");

// Both
column.Item()
    .Width(150)
    .Height(80)
    .Background(Color.Slate200)
    .Empty();
```

## Content Alignment

Align the content within a container:

```csharp
column.Item().AlignCenter().Text("Centered");
column.Item().AlignRight().Text("Right-aligned");
column.Item().AlignLeft().Text("Left-aligned");  // default
```

> **Note:** These align the entire content container, not text within a text block. For text alignment, use `.AlignLeft()`, `.AlignCenter()`, `.AlignRight()`, or `.AlignJustify()` on the `TextBlock` itself.

## Combining Styles

Styles chain naturally. The order matters — each method wraps the previous:

```csharp
column.Item()
    .Margin(0, 10, 0, 10)       // Space outside
    .Padding(15)                  // Space inside
    .Background(Color.Blue50)     // Fill color
    .Border(1, Color.Blue200)     // Edge
    .CornerRadius(8)              // Rounded corners
    .Text("Fully styled card")
    .Bold()
    .TextColor(Color.Blue800);
```

### Order of Styling

```
┌─── Margin ────────────────────────────┐
│                                       │
│  ┌─── Border ────────────────────┐    │
│  │                               │    │
│  │  ┌─── Background ────────┐   │    │
│  │  │                        │   │    │
│  │  │  ┌─── Padding ────┐   │   │    │
│  │  │  │                 │   │   │    │
│  │  │  │    Content      │   │   │    │
│  │  │  │                 │   │   │    │
│  │  │  └─────────────────┘   │   │    │
│  │  │                        │   │    │
│  │  └────────────────────────┘   │    │
│  │                               │    │
│  └───────────────────────────────┘    │
│                                       │
└───────────────────────────────────────┘
```

## Color

BonniePDF includes a comprehensive color system with the full Tailwind CSS v4 palette:

### Basic Colors

```csharp
Color.Black
Color.White
Color.Red
Color.Blue
Color.LightGray
Color.DarkGray
```

### Tailwind Palette

Each color family has shades from 50 (lightest) to 950 (darkest):

```csharp
Color.Blue50    // Very light blue
Color.Blue100
Color.Blue200
Color.Blue300
Color.Blue400
Color.Blue500   // Medium blue
Color.Blue600
Color.Blue700
Color.Blue800
Color.Blue900
Color.Blue950   // Very dark blue
```

**Available families:** `Slate`, `Gray`, `Zinc`, `Neutral`, `Stone`, `Red`, `Orange`, `Amber`, `Yellow`, `Lime`, `Green`, `Emerald`, `Teal`, `Cyan`, `Sky`, `Blue`, `Indigo`, `Violet`, `Purple`, `Fuchsia`, `Pink`, `Rose`

### Custom Colors

```csharp
// From RGB bytes (0-255)
var color = new Color(66, 133, 244);

// From hex string
var color = Color.FromHex("#4285F4");
var color = Color.FromHex("4285F4");

// From float values (0.0-1.0)
var color = Color.FromScRgb(0.26f, 0.52f, 0.96f);

// With alpha transparency
var semiTransparent = Color.Blue.WithAlpha(128);      // byte (0-255)
var semiTransparent = Color.Blue.WithAlpha(0.5f);     // float (0.0-1.0)
```

## Thickness (Margin & Padding Type)

The `Thickness` struct is used for margin and padding values:

```csharp
new Thickness(10)              // Uniform: all sides 10pt
new Thickness(20, 10)          // Horizontal 20pt, vertical 10pt
new Thickness(5, 10, 15, 20)   // Left, top, right, bottom
```

> **Note:** The fluent API accepts numeric arguments directly (`.Margin(10)`, `.Padding(20, 10)`), so you rarely need to create `Thickness` values explicitly.

## Practical Patterns

### Card Component

```csharp
column.Item()
    .Margin(0, 0, 0, 12)
    .Padding(16)
    .Background(Color.White)
    .Border(1, Color.Slate200)
    .CornerRadius(8)
    .Column(card =>
    {
        card.Item().Text("Card Title").Bold().FontSize(16);
        card.Item().Text("Card content goes here").TextColor(Color.Slate600);
    });
```

### Divider Line

```csharp
column.Item()
    .Margin(0, 16, 0, 16)
    .Height(1)
    .Background(Color.Slate200)
    .Empty();
```

### Badge

```csharp
row.AutoItem()
    .Padding(8, 4, 8, 4)
    .Background(Color.Green600)
    .CornerRadius(4)
    .Text("Active")
    .Bold()
    .FontSize(11)
    .TextColor(Color.White);
```
