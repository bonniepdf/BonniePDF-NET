using System;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Shapes;
using BonniePDF.Elements.Text;
using BonniePDF.Fluent.Descriptors;

namespace BonniePDF.Fluent.Extensions;

/// <summary>
/// Fluent extension methods for adding content elements to containers.
/// These are typically terminal methods — they create an element and end the chain,
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
    /// Adds a QR code element.
    /// </summary>
    public static QRCode QRCode(this IContentContainer c, string data, float size = 100f)
    {
        var qr = new QRCode { Data = data, Size = size };
        return c.Element(qr);
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
