using BonniePDF.Models;

namespace BonniePDF.Layout.Elements;

/// <summary>
/// Interface for elements that support margins
/// </summary>
public interface IMargin
{
    /// <summary>
    /// Gets or sets the margin around this element
    /// </summary>
    Thickness Margin { get; set; }
}