
namespace BonniePDF;

/// <summary>
/// Interface for all renderable PDF elements
/// </summary>
public interface IBackgroundColor
{
    /// <summary>The background colour, or <c>null</c> for none.</summary>
    Color? BackgroundColor { get; set; }
}