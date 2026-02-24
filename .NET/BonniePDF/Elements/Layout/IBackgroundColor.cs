using BonniePDF.Models;

namespace BonniePDF.Layout;

/// <summary>
/// Interface for all renderable PDF elements
/// </summary>
public interface IBackgroundColor
{
    Color? BackgroundColor { get; set; }
}