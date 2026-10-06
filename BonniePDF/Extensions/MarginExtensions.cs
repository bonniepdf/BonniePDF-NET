
namespace BonniePDF;

/// <summary>
/// Fluent extension methods for margins
/// </summary>
public static class MarginExtensions
{
    /// <summary>
    /// Adds uniform margin to all sides of the element
    /// </summary>
    public static T Margin<T>(this T element, float uniform) where T : IMargin
    {
        element.Margin = new Thickness(uniform);
        return element;
    }

    /// <summary>
    /// Adds horizontal and vertical margin to the element
    /// </summary>
    public static T Margin<T>(this T element, float horizontal, float vertical) where T : IMargin
    {
        element.Margin = new Thickness(horizontal, vertical);
        return element;
    }

    /// <summary>
    /// Adds individual margin values for each side of the element
    /// </summary>
    public static T Margin<T>(this T element, float left, float top, float right, float bottom) where T : IMargin
    {
        element.Margin = new Thickness(left, top, right, bottom);
        return element;
    }

    /// <summary>
    /// Adds left margin to the element
    /// </summary>
    public static T MarginLeft<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(value, m.Top, m.Right, m.Bottom);
        return element;
    }

    /// <summary>
    /// Adds right margin to the element
    /// </summary>
    public static T MarginRight<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(m.Left, m.Top, value, m.Bottom);
        return element;
    }

    /// <summary>
    /// Adds top margin to the element
    /// </summary>
    public static T MarginTop<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(m.Left, value, m.Right, m.Bottom);
        return element;
    }

    /// <summary>
    /// Adds bottom margin to the element
    /// </summary>
    public static T MarginBottom<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(m.Left, m.Top, m.Right, value);
        return element;
    }

    /// <summary>
    /// Adds horizontal margin to the element
    /// </summary>
    public static T MarginHorizontal<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(value, m.Top, value, m.Bottom);
        return element;
    }

    /// <summary>
    /// Adds vertical margin to the element
    /// </summary>
    public static T MarginVertical<T>(this T element, float value) where T : IMargin
    {
        var m = element.Margin;
        element.Margin = new Thickness(m.Left, value, m.Right, value);
        return element;
    }
}
