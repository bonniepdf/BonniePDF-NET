
namespace BonniePDF.Fluent;

/// <summary>
/// Unit-qualified overloads of the fluent measurement verbs, so any measurement
/// in a fluent chain can be written in points, inches, centimetres, or
/// millimetres.
/// </summary>
/// <remarks>
/// <para>
/// A fluent chain is a chain of verbs, so the unit travels as a second
/// argument:
/// </para>
/// <code>
/// Pdf.Create(doc =&gt; doc.Page(page =&gt; page
///     .Padding(1, Unit.Centimeter)
///     .Height(1, Unit.Inch)));
/// </code>
/// <para>
/// Each overload converts through <see cref="Units.ToPoints(float, Unit)"/> and
/// then calls the points-taking verb, so there is exactly one implementation of
/// each verb and no rounding difference between the two spellings. The
/// object-initializer API spells the same thing with the numeric extension
/// methods in <see cref="LengthExtensions"/> (<c>Height = 1.Inches()</c>).
/// </para>
/// </remarks>
public static class ContainerUnitExtensions
{
    private static float P(float value, Unit unit) => Units.ToPoints(value, unit);

    /// <summary>Sets uniform padding, in the given unit.</summary>
    public static IContentContainer Padding(this IContentContainer c, float value, Unit unit) =>
        c.Padding(P(value, unit));

    /// <summary>Sets horizontal and vertical padding, in the given unit.</summary>
    public static IContentContainer Padding(this IContentContainer c, float horizontal, float vertical, Unit unit) =>
        c.Padding(P(horizontal, unit), P(vertical, unit));

    /// <summary>Sets per-side padding, in the given unit.</summary>
    public static IContentContainer Padding(
        this IContentContainer c, float left, float top, float right, float bottom, Unit unit) =>
        c.Padding(P(left, unit), P(top, unit), P(right, unit), P(bottom, unit));

    /// <summary>Sets top padding, in the given unit.</summary>
    public static IContentContainer PaddingTop(this IContentContainer c, float value, Unit unit) =>
        c.PaddingTop(P(value, unit));

    /// <summary>Sets bottom padding, in the given unit.</summary>
    public static IContentContainer PaddingBottom(this IContentContainer c, float value, Unit unit) =>
        c.PaddingBottom(P(value, unit));

    /// <summary>Sets left padding, in the given unit.</summary>
    public static IContentContainer PaddingLeft(this IContentContainer c, float value, Unit unit) =>
        c.PaddingLeft(P(value, unit));

    /// <summary>Sets right padding, in the given unit.</summary>
    public static IContentContainer PaddingRight(this IContentContainer c, float value, Unit unit) =>
        c.PaddingRight(P(value, unit));

    /// <summary>Sets left and right padding, in the given unit.</summary>
    public static IContentContainer PaddingHorizontal(this IContentContainer c, float value, Unit unit) =>
        c.PaddingHorizontal(P(value, unit));

    /// <summary>Sets top and bottom padding, in the given unit.</summary>
    public static IContentContainer PaddingVertical(this IContentContainer c, float value, Unit unit) =>
        c.PaddingVertical(P(value, unit));

    /// <summary>Sets a uniform border width, in the given unit.</summary>
    public static IContentContainer Border(this IContentContainer c, float width, Unit unit) =>
        c.Border(P(width, unit));

    /// <summary>Sets a uniform border width and colour, in the given unit.</summary>
    public static IContentContainer Border(this IContentContainer c, float width, Unit unit, Color color) =>
        c.Border(P(width, unit), color);

    /// <summary>Sets the bottom border width, in the given unit.</summary>
    public static IContentContainer BorderBottom(
        this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderBottom(P(width, unit), color);

    /// <summary>Sets the top border width, in the given unit.</summary>
    public static IContentContainer BorderTop(this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderTop(P(width, unit), color);

    /// <summary>Sets the left border width, in the given unit.</summary>
    public static IContentContainer BorderLeft(this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderLeft(P(width, unit), color);

    /// <summary>Sets the right border width, in the given unit.</summary>
    public static IContentContainer BorderRight(
        this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderRight(P(width, unit), color);

    /// <summary>Sets the left and right border widths, in the given unit.</summary>
    public static IContentContainer BorderHorizontal(
        this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderHorizontal(P(width, unit), color);

    /// <summary>Sets the top and bottom border widths, in the given unit.</summary>
    public static IContentContainer BorderVertical(
        this IContentContainer c, float width, Unit unit, Color? color = null) =>
        c.BorderVertical(P(width, unit), color);

    /// <summary>Sets a fixed width, in the given unit.</summary>
    public static IContentContainer Width(this IContentContainer c, float value, Unit unit) =>
        c.Width(P(value, unit));

    /// <summary>Sets a fixed height, in the given unit.</summary>
    public static IContentContainer Height(this IContentContainer c, float value, Unit unit) =>
        c.Height(P(value, unit));

    /// <summary>Sets a fixed width and height, in the given unit.</summary>
    public static IContentContainer Size(this IContentContainer c, float width, float height, Unit unit) =>
        c.Size(P(width, unit), P(height, unit));

    /// <summary>Sets the corner radius, in the given unit.</summary>
    public static IContentContainer CornerRadius(this IContentContainer c, float radius, Unit unit) =>
        c.CornerRadius(P(radius, unit));

    /// <summary>Sets the page margin, in the given unit.</summary>
    public static void Margin(this PageDescriptor page, float margin, Unit unit) =>
        page.Margin(P(margin, unit));
}
