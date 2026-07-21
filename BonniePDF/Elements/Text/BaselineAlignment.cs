namespace BonniePDF.Elements.Text;

/// <summary>
/// Vertical baseline positioning for a text span. <see cref="Superscript"/>
/// raises the span above the surrounding baseline; <see cref="Subscript"/> lowers
/// it. The magnitude is derived from the span's own font size unless an explicit
/// <see cref="TextStyle.BaselineShift"/> (in points) overrides it.
/// </summary>
public enum BaselineAlignment
{
    /// <summary>Sits on the surrounding text baseline (default).</summary>
    Baseline = 0,

    /// <summary>Raised above the baseline (e.g. footnote markers, ordinals, exponents).</summary>
    Superscript = 1,

    /// <summary>Lowered below the baseline (e.g. chemical subscripts).</summary>
    Subscript = 2
}

/// <summary>
/// Helpers for resolving a <see cref="BaselineAlignment"/> into a concrete
/// vertical offset. Mirrors the TypeScript <c>BaselineAlignment</c> module so both
/// implementations shift super/subscript spans identically.
/// </summary>
public static class BaselineShiftResolver
{
    /// <summary>
    /// Fraction of the font size a superscript is raised by when no explicit
    /// <see cref="TextStyle.BaselineShift"/> is supplied.
    /// </summary>
    public const float SuperscriptShiftFraction = 0.34f;

    /// <summary>
    /// Fraction of the font size a subscript is lowered by when no explicit
    /// <see cref="TextStyle.BaselineShift"/> is supplied.
    /// </summary>
    public const float SubscriptShiftFraction = 0.16f;

    /// <summary>
    /// Resolves the vertical offset (in points, positive = up) for a span given
    /// its semantic alignment, an optional explicit shift, and the effective font
    /// size. An explicit <paramref name="explicitShift"/> always wins; otherwise a
    /// super/subscript alignment derives the shift from the font size, and
    /// <see cref="BaselineAlignment.Baseline"/> yields <c>0</c>.
    /// </summary>
    public static float Resolve(BaselineAlignment alignment, float? explicitShift, float fontSize)
    {
        if (explicitShift.HasValue) return explicitShift.Value;
        return alignment switch
        {
            BaselineAlignment.Superscript => fontSize * SuperscriptShiftFraction,
            BaselineAlignment.Subscript => -fontSize * SubscriptShiftFraction,
            _ => 0f,
        };
    }
}
