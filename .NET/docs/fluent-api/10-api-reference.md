# API Reference

Complete type and method reference for the BonniePDF Fluent API.

---

## Entry Points

### `Pdf` (Static Class)

**Namespace:** `BonniePDF.Fluent`

| Method | Returns | Description |
|---|---|---|
| `Create(Action<PdfBuilder> handler)` | `PdfBuilder` | Creates a new PDF document using the fluent builder |

### `PdfBuilder`

**Namespace:** `BonniePDF.Fluent`

| Method | Returns | Description |
|---|---|---|
| `WithMetadata(Action<DocumentMetadata>)` | `PdfBuilder` | Configures document metadata (title, author, etc.) |
| `WithSettings(Action<DocumentSettings>)` | `PdfBuilder` | Configures document settings (page size, version, etc.) |
| `WithDefaultTextStyle(Action<TextStyle>)` | `PdfBuilder` | Sets document-wide default text style |
| `Page(Action<PageDescriptor>)` | `void` | Adds a page to the document |
| `Save(string path)` | `void` | Saves PDF to file |
| `Save(Stream, bool leaveOpen = false)` | `void` | Saves PDF to stream |
| `SaveAsync(string, CancellationToken)` | `Task` | Async save to file |
| `SaveAsync(Stream, bool, CancellationToken)` | `Task` | Async save to stream |
| `GenerateBytes()` | `byte[]` | Generates PDF as byte array |

---

## Descriptors

### `PageDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Size(PdfSize)` | `void` | Sets page size |
| `Margin(float)` | `void` | Sets page margin in points |
| `Orientation(PageOrientation)` | `void` | Sets page orientation |
| `DefaultTextStyle(Action<TextStyle>)` | `void` | Sets page-level default text style |
| `Header()` | `IContentContainer` | Returns container for page header |
| `Content()` | `IContentContainer` | Returns container for page content |
| `Footer()` | `IContentContainer` | Returns container for page footer |

### `ColumnDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Spacing(float)` | `void` | Sets vertical spacing between items (points) |
| `Item()` | `IContentContainer` | Adds a new item to the column |

### `RowDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Spacing(float)` | `void` | Sets horizontal spacing between items (points) |
| `AlignItems(HorizontalAlignment)` | `void` | Sets vertical alignment of row items |
| `RelativeItem(float weight = 1f)` | `IContentContainer` | Adds proportional-width item |
| `ConstantItem(float width)` | `IContentContainer` | Adds fixed-width item |
| `AutoItem()` | `IContentContainer` | Adds auto-sizing item |

### `TableDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Columns(params float[])` | `void` | Defines column weights |
| `CellPadding(float)` | `void` | Sets default cell padding (points) |
| `RepeatHeaders(bool = true)` | `void` | Enables repeating headers on page breaks |
| `HeaderBackground(Color)` | `void` | Sets header row background color |
| `HeaderTextColor(Color)` | `void` | Sets header row text color |
| `Border(float, Color? = null)` | `void` | Sets uniform cell borders |
| `Header(Action<TableRowDescriptor>)` | `void` | Defines header row |
| `Footer(Action<TableRowDescriptor>)` | `void` | Defines footer row |
| `Row(Action<TableRowDescriptor>)` | `void` | Adds data row |
| `Row(Color, Action<TableRowDescriptor>)` | `void` | Adds data row with background |
| `Cell()` | `IContentContainer` | Returns container for next cell |

### `TableRowDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Cell()` | `IContentContainer` | Returns container for next cell in the row |

### `TextDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Span(string)` | `TextSpanDescriptor` | Adds inline text span |
| `Span(string, TextStyle)` | `void` | Adds span with predefined style |
| `Line(string)` | `TextSpanDescriptor` | Adds span followed by line break |
| `LineBreak()` | `void` | Inserts a line break |
| `CurrentPageNumber()` | `TextSpanDescriptor` | Inserts current page number |
| `TotalPages()` | `TextSpanDescriptor` | Inserts total page count |

### `TextSpanDescriptor`

**Namespace:** `BonniePDF.Fluent.Descriptors`

| Method | Returns | Description |
|---|---|---|
| `Bold()` | `TextSpanDescriptor` | Sets weight to Bold |
| `Italic()` | `TextSpanDescriptor` | Enables italic |
| `FontSize(float)` | `TextSpanDescriptor` | Sets font size (points) |
| `FontColor(Color)` | `TextSpanDescriptor` | Sets text color |
| `FontFamily(string)` | `TextSpanDescriptor` | Sets font family |
| `Underline()` | `TextSpanDescriptor` | Adds underline |
| `Strikethrough()` | `TextSpanDescriptor` | Adds strikethrough |
| `BackgroundColor(Color)` | `TextSpanDescriptor` | Sets highlight color |
| `Weight(FontWeight)` | `TextSpanDescriptor` | Sets explicit font weight |

---

## Extension Methods

### Container Element Extensions

**Namespace:** `BonniePDF.Fluent.Extensions`
**Extends:** `IContentContainer`

| Method | Returns | Description |
|---|---|---|
| `Text(string)` | `TextBlock` | Adds simple text element |
| `Text(Action<TextDescriptor>)` | `void` | Adds rich text with styled spans |
| `Empty()` | `void` | Adds empty block |
| `Column(Action<ColumnDescriptor>)` | `void` | Adds vertical column layout |
| `Row(Action<RowDescriptor>)` | `void` | Adds horizontal row layout |
| `Table(Action<TableDescriptor>)` | `void` | Adds table layout |
| `Image(string path)` | `Image` | Adds image from file |
| `Image(byte[] data)` | `Image` | Adds image from bytes |
| `Line(float thickness = 1f)` | `Line` | Adds horizontal line |
| `Line(float thickness, Color)` | `Line` | Adds colored line |
| `QRCode(string data, float size = 100f)` | `QRCode` | Adds QR code |
| `Element(IElement)` | `void` | Inserts raw element |
| `Component(IComponent)` | `void` | Composes component |
| `Component<T>()` | `void` | Composes component (parameterless ctor) |

### Container Style Extensions

**Namespace:** `BonniePDF.Fluent.Extensions`
**Extends:** `IContentContainer`
**All return:** `IContentContainer`

**Padding:**

| Method | Parameters | Description |
|---|---|---|
| `Padding(float)` | uniform | All sides |
| `Padding(float, float)` | horizontal, vertical | H and V |
| `Padding(float, float, float, float)` | L, T, R, B | Individual sides |
| `PaddingTop(float)` | top | Top only |
| `PaddingBottom(float)` | bottom | Bottom only |
| `PaddingLeft(float)` | left | Left only |
| `PaddingRight(float)` | right | Right only |
| `PaddingHorizontal(float)` | horizontal | Left and right |
| `PaddingVertical(float)` | vertical | Top and bottom |

**Background:**

| Method | Description |
|---|---|
| `Background(Color)` | Sets background color |

**Border:**

| Method | Description |
|---|---|
| `Border(float width)` | Uniform border (black) |
| `Border(float width, Color)` | Uniform border with color |
| `BorderTop(float, Color?)` | Top only |
| `BorderBottom(float, Color?)` | Bottom only |
| `BorderLeft(float, Color?)` | Left only |
| `BorderRight(float, Color?)` | Right only |

**Size:**

| Method | Description |
|---|---|
| `Width(float)` | Fixed width |
| `Height(float)` | Fixed height |

**Other:**

| Method | Description |
|---|---|
| `CornerRadius(float)` | Rounded corners |
| `Margin(float)` | Uniform margin |
| `Margin(float, float)` | Horizontal and vertical margin |
| `Margin(float, float, float, float)` | Individual side margin |
| `AlignCenter()` | Center content |
| `AlignRight()` | Right-align content |
| `AlignLeft()` | Left-align content |

### TextBlock Extensions

**Namespace:** `BonniePDF.Extensions`
**Extends:** `TextBlock`
**All return:** `TextBlock`

| Method | Description |
|---|---|
| `Font(string fontName, float? fontSize = null)` | Sets font family (and optional size) |
| `FontSize(float)` | Sets font size in points |
| `TextColor(Color)` | Sets text color |
| `Bold()` | Sets weight to Bold (700) |
| `Light()` | Sets weight to Light (300) |
| `Italic()` | Enables italic/oblique |
| `FontWeight(FontWeight)` | Sets explicit weight |
| `Underline()` | Adds underline |
| `Strikethrough()` | Adds strikethrough |
| `AlignLeft()` | Left alignment |
| `AlignCenter()` | Center alignment |
| `AlignRight()` | Right alignment |
| `AlignJustify()` | Justified alignment |

### Document Settings Extensions

**Namespace:** `BonniePDF.Fluent.Extensions`
**Extends:** `DocumentSettings`

| Method | Description |
|---|---|
| `Header(Action<int, IContentContainer>)` | Configures header template (page number + container) |
| `Footer(Action<int, IContentContainer>)` | Configures footer template (page number + container) |

---

## Configuration Types

### `DocumentSettings`

**Namespace:** `BonniePDF.Configuration`

| Property | Type | Default | Description |
|---|---|---|---|
| `PageSize` | `PdfSize` | `PdfSize.A4` | Page dimensions |
| `Orientation` | `PageOrientation` | `Portrait` | Page orientation |
| `Margin` | `float` | `50` | Page margin (points) |
| `Version` | `PdfVersion` | `Pdf14` | PDF version |
| `PdfAEnabled` | `bool` | `false` | PDF/A-1b compliance |
| `DefaultZoom` | `ZoomLevel` | `FitPage` | Viewer zoom level |
| `CustomZoomLevel` | `double` | `1.0` | Custom zoom (for `ZoomLevel.Custom`) |
| `PageMode` | `PageMode` | `UseNone` | Document display mode |
| `HideToolbar` | `bool` | `false` | Hide viewer toolbar |
| `HideMenubar` | `bool` | `false` | Hide viewer menu |
| `HideWindowUI` | `bool` | `false` | Hide window controls |
| `FitWindow` | `bool` | `false` | Fit window to page |
| `CenterWindow` | `bool` | `false` | Center window on screen |
| `DisplayDocTitle` | `bool` | `false` | Show title in title bar |
| `Watermark` | `WatermarkSettings?` | `null` | Watermark configuration |
| `Encryption` | `EncryptionSettings?` | `null` | Encryption configuration |
| `Bookmarks` | `IList<BookmarkItem>` | `[]` | Document outline items |

### `PdfSize`

**Namespace:** `BonniePDF.Configuration`

**Properties:**

| Property | Type | Description |
|---|---|---|
| `Width` | `float` | Width in points |
| `Height` | `float` | Height in points |
| `Name` | `string?` | Size name (e.g., "A4") |

**Static Sizes:**

| ISO A-Series | ISO B-Series | North American |
|---|---|---|
| `A0` (841×1189mm) | `B0` | `Letter` (8.5×11in) |
| `A1` (594×841mm) | `B1` | `Legal` (8.5×14in) |
| `A2` (420×594mm) | `B2` | `Tabloid` (11×17in) |
| `A3` (297×420mm) | `B3` | `Ledger` (17×11in) |
| `A4` (210×297mm) | `B4` | `Statement` (5.5×8.5in) |
| `A5` (148×210mm) | `B5` | `Executive` (7.25×10.5in) |

**Factory Methods:**

| Method | Description |
|---|---|
| `FromInches(float w, float h, string? name)` | Custom size from inches |
| `FromMM(float w, float h, string? name)` | Custom size from millimeters |

**Instance Methods:**

| Method | Returns | Description |
|---|---|---|
| `Portrait()` | `PdfSize` | Portrait orientation |
| `Landscape()` | `PdfSize` | Landscape orientation |

---

## Core Types

### `Color`

**Namespace:** `BonniePDF.Core`

**Constructors:**

| Constructor | Description |
|---|---|
| `Color(byte r, byte g, byte b, byte a = 255)` | From byte values |
| `Color(int r, int g, int b, int a = 255)` | From int values |
| `Color.FromHex(string hex)` | From hex string (`"#FF0000"` or `"FF0000"`) |
| `Color.FromScRgb(float r, float g, float b, float a = 1.0f)` | From float values |

**Instance Methods:**

| Method | Returns | Description |
|---|---|---|
| `WithAlpha(byte)` | `Color` | New color with alpha (0–255) |
| `WithAlpha(float)` | `Color` | New color with alpha (0.0–1.0) |
| `ToHexString()` | `string` | Hex representation (`"#RRGGBB"`) |

**Static Colors:**

Basic: `Black`, `White`, `Red`, `Blue`, `LightGray`, `DarkGray`

Tailwind v4 palette (each with shades `50`, `100`, `200`, `300`, `400`, `500`, `600`, `700`, `800`, `900`, `950`):

`Slate` · `Gray` · `Zinc` · `Neutral` · `Stone` · `Red` · `Orange` · `Amber` · `Yellow` · `Lime` · `Green` · `Emerald` · `Teal` · `Cyan` · `Sky` · `Blue` · `Indigo` · `Violet` · `Purple` · `Fuchsia` · `Pink` · `Rose`

### `Thickness`

**Namespace:** `BonniePDF.Core`

| Constructor | Parameters | Description |
|---|---|---|
| `Thickness(float)` | uniform | All sides equal |
| `Thickness(float, float)` | horizontal, vertical | H and V |
| `Thickness(float, float, float, float)` | left, top, right, bottom | Individual |

**Properties:** `Left`, `Top`, `Right`, `Bottom` (all `float`)

### `Border`

**Namespace:** `BonniePDF.Core`

| Constructor | Parameters | Description |
|---|---|---|
| `Border(float)` | uniform | All sides equal |
| `Border(float, float, float, float)` | left, top, right, bottom | Individual |

**Static Factory Methods:** `LeftOnly(float)`, `RightOnly(float)`, `TopOnly(float)`, `BottomOnly(float)`, `VerticalSides(float)`, `HorizontalSides(float)`, `None`

---

## Enums

### `FontWeight`

**Namespace:** `BonniePDF.Typography`

| Value | Numeric | CSS Equivalent |
|---|---|---|
| `Thin` | 100 | `font-weight: 100` |
| `ExtraLight` | 200 | `font-weight: 200` |
| `Light` | 300 | `font-weight: 300` |
| `Normal` | 400 | `font-weight: 400` |
| `Medium` | 500 | `font-weight: 500` |
| `SemiBold` | 600 | `font-weight: 600` |
| `Bold` | 700 | `font-weight: 700` |
| `ExtraBold` | 800 | `font-weight: 800` |
| `Black` | 900 | `font-weight: 900` |

### `PdfVersion`

**Namespace:** `BonniePDF.Configuration`

| Value | Features |
|---|---|
| `Pdf14` | Default. RC4 encryption. |
| `Pdf17` | AES-256 encryption, PDF/A-1b, XMP metadata, bookmarks. |

### `PageOrientation`

**Namespace:** `BonniePDF.Core`

`Portrait` | `Landscape`

### `TextAlignment`

**Namespace:** `BonniePDF.Elements.Text`

`Left` · `Center` · `Right` · `Middle` · `Justify`

### `TextDecoration` (Flags)

**Namespace:** `BonniePDF.Elements.Text`

`None` · `Underline` · `Strikethrough`

Combinable: `TextDecoration.Underline | TextDecoration.Strikethrough`

### `HorizontalAlignment`

**Namespace:** `BonniePDF.Core`

`Start` · `Center` · `End` · `Stretch`

### `ZoomLevel`

**Namespace:** `BonniePDF.Configuration`

`FitPage` · `FitWidth` · `FitHeight` · `Custom`

### `PageMode`

**Namespace:** `BonniePDF.Configuration`

`UseNone` · `UseOutlines` · `UseThumbs` · `FullScreen` · `UseOC` · `UseAttachments`

---

## Interfaces

### `IContentContainer`

**Namespace:** `BonniePDF.Fluent`

The core interface for the fluent chaining system. All layout methods (`.Column()`, `.Row()`, `.Table()`, `.Text()`) and styling methods (`.Padding()`, `.Background()`, `.Border()`, etc.) operate on this interface.

| Property | Type | Description |
|---|---|---|
| `Child` | `IElement?` | The child element held by this container |

### `IComponent`

**Namespace:** `BonniePDF.Fluent`

Implement this interface to create reusable document building blocks.

| Method | Description |
|---|---|
| `Compose(IContentContainer)` | Compose content into the container |

---

## TextStyle (Default Style Configuration)

**Namespace:** `BonniePDF.Elements.Text`

Used with `WithDefaultTextStyle()` and `page.DefaultTextStyle()` to set inheritable defaults.

**Configuration Methods (all return `TextStyle`):**

| Method | Description |
|---|---|
| `WithFamily(string)` | Set font family |
| `WithSize(float)` | Set font size |
| `WithColor(Color)` | Set text color |
| `WithWeight(FontWeight)` | Set font weight |
| `WithBackground(Color)` | Set background highlight |
| `Bold()` | Set weight to Bold |
| `Light()` | Set weight to Light |
| `Thin()` | Set weight to Thin |
| `WithItalic()` | Enable italic |
| `Underline()` | Add underline decoration |
| `Strikethrough()` | Add strikethrough decoration |

**Properties:**

| Property | Type | Default | Description |
|---|---|---|---|
| `FontFamily` | `string` | `null` | Base font family |
| `Weight` | `FontWeight` | `Normal` | Font weight |
| `Italic` | `bool` | `false` | Italic rendering |
| `FontSize` | `float?` | `null` | Font size in points |
| `Color` | `Color?` | `null` | Text color |
| `BackgroundColor` | `Color?` | `null` | Background highlight |
| `Decoration` | `TextDecoration` | `None` | Text decoration |
