using System;
using System.Globalization;

namespace BonniePDF.Core;

/// <summary>
/// Unit-qualified measurements for object-initializer authoring.
/// </summary>
/// <remarks>
/// <para>
/// Every measurement in BonniePDF is a number of points, and any measurement may
/// be written in points, inches, centimetres, or millimetres. In an object
/// initializer the unit travels as a call on the number itself:
/// </para>
/// <code>
/// new Container { Height = 1.Inches(), Padding = new Thickness(10.Mm()) }
/// </code>
/// <para>
/// The fluent API spells the same thing as a second argument -
/// <c>.Height(1, Unit.Inch)</c> - because a fluent chain is a chain of verbs.
/// Both resolve through <see cref="Units.ToPoints(float, Unit)"/>, so there is
/// one conversion table and no rounding difference between the two spellings.
/// </para>
/// </remarks>
public static class LengthExtensions
{
    /// <summary>Interprets this number as points (1/72 inch) - the identity conversion.</summary>
    public static float Points(this float value) => Units.ToPoints(value, Unit.Point);

    /// <inheritdoc cref="Points(float)"/>
    public static float Points(this int value) => Units.ToPoints(value, Unit.Point);

    /// <inheritdoc cref="Points(float)"/>
    public static float Points(this double value) => Units.ToPoints((float)value, Unit.Point);

    /// <summary>Converts this number of inches to points.</summary>
    public static float Inches(this float value) => Units.ToPoints(value, Unit.Inch);

    /// <inheritdoc cref="Inches(float)"/>
    public static float Inches(this int value) => Units.ToPoints(value, Unit.Inch);

    /// <inheritdoc cref="Inches(float)"/>
    public static float Inches(this double value) => Units.ToPoints((float)value, Unit.Inch);

    /// <summary>Converts this number of centimetres to points.</summary>
    public static float Cm(this float value) => Units.ToPoints(value, Unit.Centimeter);

    /// <inheritdoc cref="Cm(float)"/>
    public static float Cm(this int value) => Units.ToPoints(value, Unit.Centimeter);

    /// <inheritdoc cref="Cm(float)"/>
    public static float Cm(this double value) => Units.ToPoints((float)value, Unit.Centimeter);

    /// <summary>Converts this number of millimetres to points.</summary>
    public static float Mm(this float value) => Units.ToPoints(value, Unit.Millimeter);

    /// <inheritdoc cref="Mm(float)"/>
    public static float Mm(this int value) => Units.ToPoints(value, Unit.Millimeter);

    /// <inheritdoc cref="Mm(float)"/>
    public static float Mm(this double value) => Units.ToPoints((float)value, Unit.Millimeter);

    /// <summary>Converts this number to points, in the given unit.</summary>
    public static float In(this float value, Unit unit) => Units.ToPoints(value, unit);

    /// <inheritdoc cref="In(float, Unit)"/>
    public static float In(this int value, Unit unit) => Units.ToPoints(value, unit);
}
