namespace BonniePDF.Elements.Tables;

/// <summary>
/// A <b>layout grid</b>: arranges content in weighted columns and aligned rows for
/// visual layout — address blocks, totals boxes, forms, side-by-side panels — as
/// opposed to tabular <em>data</em>.
///
/// <para>
/// <see cref="Grid"/> reuses <see cref="Table"/>'s column-weight, pagination, and
/// cell engine (padding, borders, backgrounds, per-cell alignment), so it is the
/// right tool when columns must stay aligned across multiple rows. What differs is
/// <b>meaning</b>: a <see cref="Grid"/> is tagged in the PDF structure tree as a
/// generic <c>Div</c> grouping, and each cell's content tags itself in reading
/// order. It therefore carries no table semantics — no phantom "table with N
/// columns" announced to assistive tech, and no PDF/UA §7.5 header requirement. It
/// is accessible by construction.
/// </para>
///
/// <para>
/// Choosing the right element:
/// <list type="bullet">
/// <item>A single row of columns (a key/value pair, a label + amount) → prefer
/// <c>HorizontalStack</c> with <c>WidthDistribution.Weighted</c>; it is lighter.</item>
/// <item>Multiple rows whose columns must line up (a form, an invoice details block,
/// side-by-side panels) → <see cref="Grid"/>.</item>
/// <item>Actual tabular data (rows and columns with header relationships) →
/// <see cref="Table"/>, and mark its header cells so it emits <c>TH</c>.</item>
/// </list>
/// </para>
///
/// <para>
/// <see cref="Grid"/> pins the layout-grid tagging on, so — unlike
/// <see cref="Table.Presentation"/> — it can never be flipped back into a data
/// table by mistake.
/// </para>
/// </summary>
public class Grid : Table
{
    /// <inheritdoc />
    public Grid(params float[] columnWeights) : base(columnWeights)
    {
    }

    /// <inheritdoc /> — always a layout grid (<c>Div</c>), independent of <see cref="Table.Presentation"/>.
    protected override bool IsLayoutGrid => true;
}
