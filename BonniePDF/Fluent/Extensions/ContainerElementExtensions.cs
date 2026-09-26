using System;
using BonniePDF.Core;
using BonniePDF.Drawing;
using BonniePDF.Elements;
using BonniePDF.Elements.Drawing;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Fluent.Descriptors;

namespace BonniePDF.Fluent.Extensions;

/// <summary>
/// Fluent extension methods for adding content elements to containers.
/// These are typically terminal methods - they create an element and end the chain,
/// or return the concrete element type for element-specific styling.
/// </summary>
public static class ContainerElementExtensions
{
    // --- Text ---

    /// <summary>
    /// Adds a simple text element. Returns the TextBlock for further styling
    /// via existing TextBlockExtensions (.Bold(), .FontSize(), .AlignCenter(), etc.).
    /// </summary>
    public static TextBlock Text(this IContentContainer c, string text)
    {
        var textBlock = new TextBlock { Text = text };
        return c.Element(textBlock);
    }

    /// <summary>
    /// Adds rich text with multiple styled spans.
    /// Use the TextDescriptor to configure spans, page numbers, and formatting.
    /// </summary>
    public static void Text(this IContentContainer c, Action<TextDescriptor> handler)
    {
        var descriptor = new TextDescriptor();
        handler(descriptor);
        c.Element<RichText>(descriptor.RichText);
    }

    // --- Empty ---

    /// <summary>
    /// Adds an empty block to the container, which terminates the chain.
    /// Useful for colored spacer blocks or drawing backgrounds without content.
    /// </summary>
    public static void Empty(this IContentContainer c)
    {
        c.Element(new Elements.Layout.Container());
    }

    /// <summary>
    /// Adds an invisible spacer that reserves the specified vertical space in points.
    /// </summary>
    public static void Spacer(this IContentContainer c, float height)
    {
        c.Element(new Elements.Layout.Spacer(height));
    }

    /// <summary>
    /// Forces a page break. The next element after this will start at the top of a new page.
    /// Has no effect if the current position is already at the top of a page.
    /// </summary>
    public static void PageBreak(this IContentContainer c)
    {
        c.Element(new Elements.Layout.PageBreak());
    }

    // --- Layout ---

    /// <summary>
    /// Adds a vertical column layout. Items are stacked top to bottom with automatic pagination.
    /// </summary>
    public static void Column(this IContentContainer c, Action<ColumnDescriptor> handler)
    {
        var descriptor = new ColumnDescriptor();
        handler(descriptor);
        c.Element<VerticalStack>(descriptor.Stack);
    }

    /// <summary>
    /// Adds a horizontal row layout. Items are placed side by side.
    /// </summary>
    public static void Row(this IContentContainer c, Action<RowDescriptor> handler)
    {
        var descriptor = new RowDescriptor();
        handler(descriptor);

        // If the container has an explicit height, transfer it to the stack
        // so that vertical alignment (AlignItems) works correctly within that height.
        if (c is Container container && container.Height.HasValue)
        {
            descriptor.Stack.Height = container.Height.Value 
                - container.Padding.Top - container.Padding.Bottom 
                - container.Border.VerticalWidth;
        }

        c.Element<HorizontalStack>(descriptor.Stack);
    }

    /// <summary>
    /// Adds a table layout with columns, headers, footers, and auto-flowing cells.
    /// </summary>
    public static void Table(this IContentContainer c, Action<TableDescriptor> handler)
    {
        var descriptor = new TableDescriptor();
        handler(descriptor);
        c.Element<Elements.Tables.Table>(descriptor.Build());
    }

    // --- Media ---

    /// <summary>
    /// Adds an image from a file path. Returns the Image for further styling
    /// via existing ImageExtensions (.WithWidth(), .WithFitMode(), etc.).
    /// </summary>
    public static Image Image(this IContentContainer c, string path)
    {
        var image = new Image { FilePath = path };
        return c.Element(image);
    }

    /// <summary>
    /// Adds an image from raw byte data.
    /// </summary>
    public static Image Image(this IContentContainer c, byte[] data)
    {
        var image = new Image { ImageData = data };
        return c.Element(image);
    }

    /// <summary>
    /// Adds an SVG file drawn as vector content. Returns the Svg for further
    /// configuration; for markup, add <c>new Svg { SvgData = ... }</c> with
    /// <c>Element</c>.
    /// </summary>
    public static Svg Svg(this IContentContainer c, string path)
    {
        var svg = new Svg { FilePath = path };
        return c.Element(svg);
    }

    // --- Shapes ---

    /// <summary>
    /// Adds a horizontal line.
    /// </summary>
    public static Line Line(this IContentContainer c, float thickness = 1f)
    {
        var line = new Line { Thickness = thickness };
        return c.Element(line);
    }

    /// <summary>
    /// Adds a horizontal line with color.
    /// </summary>
    public static Line Line(this IContentContainer c, float thickness, Color color)
    {
        var line = new Line { Thickness = thickness, Color = color };
        return c.Element(line);
    }

    /// <summary>
    /// Adds a filled ellipse.
    /// </summary>
    public static Ellipse Ellipse(this IContentContainer c, float width, float height)
    {
        var el = new Ellipse { Width = width, Height = height };
        return c.Element(el);
    }

    /// <summary>
    /// Adds a filled ellipse with a specific color.
    /// </summary>
    public static Ellipse Ellipse(this IContentContainer c, float width, float height, Color color)
    {
        var el = new Ellipse { Width = width, Height = height, FillColor = color };
        return c.Element(el);
    }

    /// <summary>
    /// Adds a QR code element.
    /// </summary>
    public static QRCode QRCode(this IContentContainer c, string data, float size = 100f)
    {
        var qr = new QRCode { Data = data, Size = size };
        return c.Element(qr);
    }

    /// <summary>
    /// Adds a 1D barcode element (Code 128 by default).
    /// </summary>
    public static Barcode Barcode(this IContentContainer c, string value,
        BarcodeSymbology symbology = BarcodeSymbology.Code128)
    {
        var barcode = new Barcode { Value = value, Symbology = symbology };
        return c.Element(barcode);
    }

    /// <summary>
    /// Adds a Data Matrix (ECC200) element.
    /// </summary>
    public static DataMatrix DataMatrix(this IContentContainer c, string data, float size = 100f)
    {
        var dm = new DataMatrix { Data = data, Size = size };
        return c.Element(dm);
    }

    // --- Canvas ---

    /// <summary>
    /// Adds a canvas element for freeform drawing.
    /// The handler receives (canvas, width, height).
    /// </summary>
    public static CanvasElement Canvas(this IContentContainer c, float height, Action<ICanvas, float, float> handler)
    {
        var canvas = new CanvasElement(handler) { CanvasHeight = height };
        return c.Element(canvas);
    }

    // --- Raw Element Escape Hatch ---

    /// <summary>
    /// Inserts any raw IElement into the fluent tree.
    /// Use this to integrate elements from the standard API (charts, custom elements, etc.).
    /// </summary>
    public static void Element(this IContentContainer c, IElement element)
    {
        c.Element<IElement>(element);
    }

    // --- Component ---

    /// <summary>
    /// Composes a reusable component into this container.
    /// </summary>
    public static void Component(this IContentContainer c, IComponent component)
    {
        component.Compose(c);
    }

    /// <summary>
    /// Composes a reusable component (created via parameterless constructor) into this container.
    /// </summary>
    public static void Component<T>(this IContentContainer c) where T : IComponent, new()
    {
        new T().Compose(c);
    }
}
