using System;
using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// Represents a single page in a PDF document
/// </summary>
public class Page
{
    private readonly List<IElement> _elements = new();

    /// <summary>
    /// Creates a new instance of Page with the specified dimensions and margin
    /// </summary>
    public Page(float width, float height, float margin)
    {
        Size = new PdfSize(width, height);
        Margin = margin;
        HasExplicitMargin = true;
    }

    /// <summary>
    /// Creates a new instance of Page with the specified page size and margin
    /// </summary>
    public Page(PdfSize pageSize, float margin)
    {
        Size = pageSize;
        Margin = margin;
        HasExplicitMargin = true;
    }

    /// <summary>
    /// Creates a page that inherits its margin from
    /// <see cref="DocumentSettings.Margin"/> at render time. <paramref name="fallbackMargin"/>
    /// seeds <see cref="Margin"/> so the property stays meaningful before rendering.
    /// </summary>
    internal Page(PdfSize pageSize, float fallbackMargin, bool inheritMargin)
    {
        Size = pageSize;
        Margin = fallbackMargin;
        HasExplicitMargin = !inheritMargin;
    }

    /// <summary>
    /// Whether this page carries a margin of its own. When <c>false</c> the
    /// renderer takes <see cref="DocumentSettings.Margin"/> instead, so settings
    /// assigned after the document was constructed still reach the first page.
    /// </summary>
    internal bool HasExplicitMargin { get; set; }

    /// <summary>
    /// Gets the elements on this page
    /// </summary>
    public IReadOnlyList<IElement> Elements => _elements.AsReadOnly();

    /// <summary>
    /// Gets the page size
    /// </summary>
    public PdfSize Size { get; }

    /// <summary>
    /// Background colour for this page only. When set, the render pipeline
    /// fills the entire page (ignoring margins) with this colour behind
    /// everything else, overriding <see cref="DocumentSettings.PageColor"/> for
    /// this page. When <c>null</c>, the document-level
    /// <see cref="DocumentSettings.PageColor"/> (if any) is used. Leave both
    /// <c>null</c> for a transparent (viewer-default) background.
    /// </summary>
    public Color? PageColor { get; set; }

    /// <summary>
    /// Full-page background element for this page only (drawn behind all
    /// content, ignoring margins). When set, the pipeline renders this element
    /// instead of calling <see cref="DocumentSettings.PageBackground"/> for this
    /// page (and any continuation pages its content overflows onto). Useful for
    /// per-page letterhead, cover images, or section dividers. Leave
    /// <c>null</c> to fall back to the document-level
    /// <see cref="DocumentSettings.PageBackground"/> template.
    /// </summary>
    public IElement? PageBackground { get; set; }

    /// <summary>
    /// Full-page foreground element for this page only (drawn on top of all
    /// content, ignoring margins). When set, the pipeline renders this element
    /// instead of calling <see cref="DocumentSettings.PageForeground"/> for this
    /// page (and any continuation pages). Useful for per-page
    /// "DRAFT"/"CONFIDENTIAL" overlays. Leave <c>null</c> to fall back to the
    /// document-level <see cref="DocumentSettings.PageForeground"/> template.
    /// </summary>
    public IElement? PageForeground { get; set; }

    /// <summary>
    /// Gets the page width in points
    /// </summary>
    public float Width => Size.Width;

    /// <summary>
    /// Gets the page height in points
    /// </summary>
    public float Height => Size.Height;

    /// <summary>
    /// Gets the page margin in points
    /// </summary>
    public float Margin { get; }

    /// <summary>
    /// Gets the available content width (page width minus margins)
    /// </summary>
    public float ContentWidth => Width - Margin * 2;

    /// <summary>
    /// Gets the available content height (page height minus margins)
    /// </summary>
    public float ContentHeight => Height - Margin * 2;

    /// <summary>
    /// Adds an element to this page
    /// </summary>
    public void Add(IElement element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        _elements.Add(element);
    }

    /// <summary>
    /// Measures the total height of all elements on this page
    /// </summary>
    public float MeasureContentHeight()
    {
        float totalHeight = 0;
        foreach (IElement element in _elements) totalHeight += element.MeasureHeight(ContentWidth);
        return totalHeight;
    }

    /// <summary>
    /// Checks if the content fits within the available height
    /// </summary>
    public bool ContentFits()
    {
        return MeasureContentHeight() <= ContentHeight;
    }
}