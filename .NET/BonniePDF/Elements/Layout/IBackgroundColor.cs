using BonniePDF.Core;

namespace BonniePDF.Elements.Layout;

/// <summary>
/// Interface for all renderable PDF elements
/// </summary>
public interface IBackgroundColor
{
    Color? BackgroundColor { get; set; }
}