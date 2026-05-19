using System;
using System.Collections.Generic;
using BonniePDF.Configuration;

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
    }

    /// <summary>
    /// Creates a new instance of Page with the specified page size and margin
    /// </summary>
    public Page(PdfSize pageSize, float margin)
    {
        Size = pageSize;
        Margin = margin;
    }

    /// <summary>
    /// Gets the elements on this page
    /// </summary>
    public IReadOnlyList<IElement> Elements => _elements.AsReadOnly();

    /// <summary>
    /// Gets the page size
    /// </summary>
    public PdfSize Size { get; }

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