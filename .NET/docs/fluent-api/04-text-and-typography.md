# Text & Typography

BonniePDF offers two levels of text rendering: **simple text** (a single styled block) and **rich text** (multiple styled spans within a paragraph).

## Simple Text

The most common way to add text. Returns a `TextBlock` for chained styling:

```csharp
column.Item()
    .Text("Hello, World!")
    .Bold()
    .FontSize(24)
    .TextColor(Color.Slate800);
```

### Text Styling Methods

All methods return `TextBlock` for continued chaining:

| Method | Description |
|---|---|
| `.Font(string fontName)` | Sets font family for weight/style resolution |
| `.FontSize(float size)` | Sets font size in points |
| `.TextColor(Color color)` | Sets text color |
| `.Bold()` | Sets font weight to Bold (700) |
| `.Light()` | Sets font weight to Light (300) |
| `.Italic()` | Enables italic/oblique rendering |
| `.FontWeight(FontWeight weight)` | Sets explicit font weight (100–900) |
| `.Underline()` | Adds underline decoration |
| `.Strikethrough()` | Adds strikethrough decoration |
| `.AlignLeft()` | Left-aligned text (default) |
| `.AlignCenter()` | Center-aligned text |
| `.AlignRight()` | Right-aligned text |
| `.AlignJustify()` | Justified text (both edges aligned) |
| `.Margin(...)` | Sets margin around the text block |

### Font Weight

BonniePDF uses a CSS/OpenType-compatible weight scale:

```csharp
column.Item().Text("Thin text").FontWeight(FontWeight.Thin);           // 100
column.Item().Text("Light text").Light();                               // 300
column.Item().Text("Normal text");                                      // 400 (default)
column.Item().Text("Medium text").FontWeight(FontWeight.Medium);       // 500
column.Item().Text("Semi-bold text").FontWeight(FontWeight.SemiBold);  // 600
column.Item().Text("Bold text").Bold();                                 // 700
column.Item().Text("Extra-bold text").FontWeight(FontWeight.ExtraBold); // 800
column.Item().Text("Black text").FontWeight(FontWeight.Black);         // 900
```

| Weight | Value | Shorthand |
|---|---|---|
| `FontWeight.Thin` | 100 | — |
| `FontWeight.ExtraLight` | 200 | — |
| `FontWeight.Light` | 300 | `.Light()` |
| `FontWeight.Normal` | 400 | default |
| `FontWeight.Medium` | 500 | — |
| `FontWeight.SemiBold` | 600 | — |
| `FontWeight.Bold` | 700 | `.Bold()` |
| `FontWeight.ExtraBold` | 800 | — |
| `FontWeight.Black` | 900 | — |

> **How it works:** When you call `.Bold()` or `.FontWeight()`, BonniePDF automatically resolves the correct variant font. For example, `Helvetica` + `Bold` resolves to `Helvetica-Bold`. For standard PDF fonts, weights map to their nearest available variant (Normal or Bold).

### Font Families

BonniePDF includes the 14 standard PDF fonts that are available in every PDF reader:

**Sans-Serif:**
```csharp
.Font(StandardFonts.Helvetica)           // Helvetica (default)
.Font(StandardFonts.HelveticaBold)       // Helvetica Bold
.Font(StandardFonts.HelveticaOblique)    // Helvetica Oblique
.Font(StandardFonts.HelveticaBoldOblique) // Helvetica Bold Oblique
```

**Serif:**
```csharp
.Font(StandardFonts.TimesRoman)          // Times Roman
.Font(StandardFonts.TimesBold)           // Times Bold
.Font(StandardFonts.TimesItalic)         // Times Italic
.Font(StandardFonts.TimesBoldItalic)     // Times Bold Italic
```

**Monospace:**
```csharp
.Font(StandardFonts.Courier)             // Courier
.Font(StandardFonts.CourierBold)         // Courier Bold
.Font(StandardFonts.CourierOblique)      // Courier Oblique
.Font(StandardFonts.CourierBoldOblique)  // Courier Bold Oblique
```

**Special:**
```csharp
.Font(StandardFonts.Symbol)              // Symbol characters
.Font(StandardFonts.ZapfDingbats)        // Dingbats/icons
```

### Semantic vs. Direct Font Selection

You can set fonts in two ways:

**Direct** — specify the exact variant:
```csharp
column.Item().Text("Bold title").Font(StandardFonts.HelveticaBold);
```

**Semantic** — set the family and let weight/style resolve automatically:
```csharp
column.Item().Text("Bold title").Font(StandardFonts.Helvetica).Bold();
// or with a default style set:
column.Item().Text("Bold title").Bold();
```

The semantic approach is preferred because:
- You can change the weight independently of the family
- The default text style applies the base family, so you only write `.Bold()`
- It's more readable and mirrors CSS font-weight behavior

### Custom Fonts (TTF Embedding)

Embed TrueType fonts by passing the file path:

```csharp
column.Item()
    .Text("Custom font text")
    .Font("Fonts/Roboto-Regular.ttf")
    .FontSize(14);
```

BonniePDF embeds the font subset automatically, ensuring the PDF renders correctly on any system.

## Rich Text

For paragraphs with mixed formatting (bold words, colored phrases, etc.), use the `TextDescriptor` overload:

```csharp
column.Item().Text(text =>
{
    text.Span("This is ")
        .FontSize(12);

    text.Span("bold and red")
        .Bold()
        .FontColor(Color.Red);

    text.Span(" within the same paragraph.")
        .FontSize(12);
});
```

### TextDescriptor Methods

| Method | Returns | Description |
|---|---|---|
| `text.Span(string)` | `TextSpanDescriptor` | Adds an inline text span |
| `text.Span(string, TextStyle)` | void | Adds a span with a predefined style |
| `text.Line(string)` | `TextSpanDescriptor` | Adds a span followed by a line break |
| `text.LineBreak()` | void | Inserts a line break |
| `text.CurrentPageNumber()` | `TextSpanDescriptor` | Inserts the current page number |
| `text.TotalPages()` | `TextSpanDescriptor` | Inserts the total page count |

### TextSpanDescriptor Styling

Each span can be independently styled:

```csharp
text.Span("Styled text")
    .Bold()
    .Italic()
    .FontSize(14)
    .FontColor(Color.Blue)
    .FontFamily(StandardFonts.Helvetica)
    .Underline()
    .Strikethrough()
    .BackgroundColor(Color.Yellow100)
    .Weight(FontWeight.SemiBold);
```

### Rich Text Example: Legal Disclaimer

```csharp
column.Item().Text(text =>
{
    text.Span("IMPORTANT: ").Bold().FontColor(Color.Red600);
    text.Span("By using this software, you agree to the ");
    text.Span("Terms of Service").Bold().Underline().FontColor(Color.Blue600);
    text.Span(" and ");
    text.Span("Privacy Policy").Bold().Underline().FontColor(Color.Blue600);
    text.Span(". All rights reserved.");
});
```

### Page Number Tokens

Insert dynamic page numbers in any text:

```csharp
column.Item().Text(text =>
{
    text.Span("Page ");
    text.CurrentPageNumber().Bold();
    text.Span(" of ");
    text.TotalPages().Bold();
});
```

## Text Decorations

Decorations can be combined:

```csharp
// Underline only
column.Item().Text("Underlined").Underline();

// Strikethrough only
column.Item().Text("Struck through").Strikethrough();

// Both combined
column.Item().Text("Both decorations").Underline().Strikethrough();
```

## Text Alignment

```csharp
column.Item().Text("Left aligned").AlignLeft();       // default
column.Item().Text("Center aligned").AlignCenter();
column.Item().Text("Right aligned").AlignRight();
column.Item().Text("Justified text fills the full width evenly").AlignJustify();
```

## Default Text Style

Set once, apply everywhere:

```csharp
doc.WithDefaultTextStyle(style => style
    .WithFamily(StandardFonts.Helvetica)
    .WithSize(11)
    .WithColor(Color.Slate700));

doc.Page(page =>
{
    page.Content().Column(column =>
    {
        // Inherits Helvetica 11pt Slate700 — no styling needed
        column.Item().Text("Body paragraph");

        // Override only what changes
        column.Item().Text("Heading").Bold().FontSize(20).TextColor(Color.Slate800);

        // Italic inherits family and size, just changes style
        column.Item().Text("Emphasis").Italic();
    });
});
```
