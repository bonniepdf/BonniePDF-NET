using System;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;

namespace BonniePDF.Fluent.Descriptors;

/// <summary>
/// Describes a page's configuration and structure.
/// Provides scoped methods for page size, margins, headers, content, and footers.
/// </summary>
public sealed class PageDescriptor
{
    internal PdfSize PageSize { get; private set; } = PdfSize.A4;
    internal float PageMargin { get; private set; } = 50f;
    internal PageOrientation PageOrientation { get; private set; } = PageOrientation.Portrait;
    internal ContentSlot HeaderSlot { get; } = new();
    internal ContentSlot ContentSlot { get; } = new();
    internal ContentSlot FooterSlot { get; } = new();
    internal TextStyle DefaultStyle { get; private set; }

    /// <summary>
    /// Sets the page size.
    /// </summary>
    public void Size(PdfSize size) => PageSize = size;

    /// <summary>
    /// Sets the page margin in points.
    /// </summary>
    public void Margin(float margin) => PageMargin = margin;

    /// <summary>
    /// Sets the page orientation.
    /// </summary>
    public void Orientation(PageOrientation orientation) => PageOrientation = orientation;

    /// <summary>
    /// Configures the default text style for all text on this page.
    /// Overrides any document-level default set via <see cref="PdfBuilder.WithDefaultTextStyle"/>.
    /// Text elements created after this call will inherit these defaults.
    /// </summary>
    public void DefaultTextStyle(Action<TextStyle> configure)
    {
        DefaultStyle = new TextStyle();
        configure(DefaultStyle);

        // Set the ambient so text elements created after this call inherit the page style
        TextElementBase.AmbientDefaultStyle.Value = DefaultStyle;
    }

    /// <summary>
    /// Returns a container for the page header.
    /// Content placed here appears at the top of every page.
    /// </summary>
    public IContentContainer Header() => HeaderSlot;

    /// <summary>
    /// Returns a container for the main page content.
    /// This is where the bulk of the document content goes.
    /// </summary>
    public IContentContainer Content() => ContentSlot;

    /// <summary>
    /// Returns a container for the page footer.
    /// Content placed here appears at the bottom of every page.
    /// </summary>
    public IContentContainer Footer() => FooterSlot;
}
