using System;
using System.Collections.Generic;
using System.Linq;

namespace BonniePDF;

/// <summary>
/// A table element with support for column weights, cell padding, borders, headers, and footers
/// </summary>
public class Table : BorderedElementBase, IStatefulElement, IRoleProvider, IBorderFrameHost
{
    /// <summary>
    /// When <c>true</c>, the table is a <b>layout (presentation) table</b>: it
    /// arranges content in a grid with no tabular data relationships. Per
    /// ISO 14289-1 §7.5 / Matterhorn checkpoint 15, such a table MUST NOT be
    /// tagged with table structure — so it is tagged as a generic <c>Div</c>
    /// grouping and each cell's content is tagged with its own natural role in
    /// row-major reading order (no <c>Table</c>/<c>THead</c>/<c>TR</c>/<c>TH</c>/
    /// <c>TD</c>, and <c>AddHeader</c>/<c>IsHeader</c> are ignored for tagging).
    /// Visual output is unchanged. Default <c>false</c> (a semantic data table).
    /// </summary>
    public bool Presentation { get; set; }

    /// <summary>
    /// Whether this element tags as a layout grid (a <c>Div</c> grouping) rather
    /// than a data table. Routed through a protected virtual so <see cref="Grid"/>
    /// can pin it on; defaults to the public <see cref="Presentation"/> escape hatch.
    /// </summary>
    protected virtual bool IsLayoutGrid => Presentation;

    /// <summary>The grouping role this table tags under: Table for data, Div for layout.</summary>
    private SemanticRole GroupRole => IsLayoutGrid ? SemanticRole.Div : SemanticRole.Table;

    /// <summary><c>Table</c>, or <c>Div</c> when the table is used only as a layout grid.</summary>
    public SemanticRole SemanticRole => GroupRole;

    private readonly float[] _columnWeights;
    private readonly List<TableRow> _footers = new();
    private readonly List<TableRow> _headers = new();
    private readonly TableRowStore _rows = new();

    // State tracking for multipage rendering
    private int _lastRowRendered = -1;
    private bool _headersPending = true;
    /// <summary>The running values of the body rows drawn so far, for the carried-forward total.</summary>
    private readonly List<decimal> _runningValues = [];
    /// <summary>A page ended with a carried-forward row, so the next page opens with a brought-forward one.</summary>
    private bool _carryPending;

    /// <summary>The document's default text style, handed over by the style cascade for the rows the table makes itself.</summary>
    /// <remarks>The running-total rows are built during layout, after the cascade has visited the tree.</remarks>
    internal TextStyle? CascadedTextStyle { get; set; }

    public Table(params float[] columnWeights)
    {
        _columnWeights = columnWeights;
    }

    internal IReadOnlyList<float> ColumnWeights => _columnWeights;
    internal IReadOnlyList<TableRow> HeaderRows => _headers;
    internal IReadOnlyList<TableRow> FooterRows => _footers;

    /// <summary>
    /// Default padding for all cells
    /// </summary>
    public float CellPadding { get; set; } = 5;

    /// <summary>
    /// Whether to repeat headers on each new page
    /// </summary>
    public bool RepeatHeadersOnNewPage { get; set; } = true;

    /// <summary>
    /// Whether to show footers on each page (only last page if false)
    /// </summary>
    public bool ShowFootersOnAllPages { get; set; }

    /// <summary>
    /// Turns on a running total: the column (0-based, at least 1) that shows it. When the body breaks
    /// across pages, each page that continues ends with a "carried forward" row holding the total of
    /// the rows' <see cref="TableRow.RunningValue"/> so far, and the next page opens, after any repeated
    /// header, with a "brought forward" row holding the same total. The label spans the columns before
    /// this one. Both rows are drawn in the footer colours and left out of the accessibility structure,
    /// since they repeat what the rows already say. Null (the default) turns the feature off.
    /// </summary>
    public int? RunningTotalColumn { get; set; }

    /// <summary>Label of the row that ends a page that continues. Default <c>"Carried forward"</c>.</summary>
    public string CarriedForwardLabel { get; set; } = "Carried forward";

    /// <summary>Label of the row that opens a continuation page. Default <c>"Brought forward"</c>.</summary>
    public string BroughtForwardLabel { get; set; } = "Brought forward";

    /// <summary>Number format of the running total: <c>G</c>, <c>F&lt;n&gt;</c>, <c>N&lt;n&gt;</c> or <c>P&lt;n&gt;</c>, as for charts. Default <c>"N2"</c>.</summary>
    public string RunningTotalFormat { get; set; } = "N2";

    /// <summary>Text written before the running total, such as a currency symbol. Default empty.</summary>
    public string RunningTotalPrefix { get; set; } = "";

    /// <summary>
    /// Default background color for header rows
    /// </summary>
    public Color? HeaderBackgroundColor { get; set; }

    /// <summary>
    /// Default text color for header rows
    /// </summary>
    public Color? HeaderTextColor { get; set; }

    /// <summary>
    /// Default background color for footer rows
    /// </summary>
    public Color? FooterBackgroundColor { get; set; }

    /// <summary>
    /// Default text color for footer rows
    /// </summary>
    public Color? FooterTextColor { get; set; }

    /// <summary>
    /// Default background color for data rows
    /// </summary>
    public Color? RowBackgroundColor { get; set; }

    /// <summary>
    /// Default text color for data rows
    /// </summary>
    public Color? RowTextColor { get; set; }

    /// <summary>
    /// The data rows in this table (exposed for collection initializer syntax)
    /// </summary>
    public IList<TableRow> Rows => _rows;

    /// <summary>Returns all rows (headers + data rows + footers) for tree-walking.</summary>
    public IReadOnlyList<TableRow> GetAllRows() =>
        _headers.Concat(_rows).Concat(_footers).ToList().AsReadOnly();

    /// <summary>Returns the content of every cell across the header, body and footer rows.</summary>
    public IEnumerable<IElement> GetChildren()
    {
        // Read without materialising compact body rows (see TableRowStore), into a list sized
        // for every cell, so a long table's walk does not regrow it.
        int cells = 0;
        foreach (TableRow row in _headers) cells += row.Cells.Length;
        for (int i = 0; i < _rows.Count; i++) cells += _rows.CellCount(i);
        foreach (TableRow row in _footers) cells += row.Cells.Length;
        var children = new List<IElement>(cells);
        foreach (TableRow row in _headers)
            foreach (TableCell cell in row.Cells)
                if (cell.Content is not null) children.Add(cell.Content);
        for (int i = 0; i < _rows.Count; i++) _rows.AddContentsTo(i, children);
        foreach (TableRow row in _footers)
            foreach (TableCell cell in row.Cells)
                if (cell.Content is not null) children.Add(cell.Content);
        return children;
    }

    /// <summary>Adds an empty row.</summary>
    public void AddRow() => AddRow(Array.Empty<IElement>());

    /// <summary>Adds a row of text cells.</summary>
    public void AddRow(params string[] cells) => AddRow((IEnumerable<string>)cells);

    /// <summary>Adds a row of text cells.</summary>
    public void AddRow(IEnumerable<string> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        AddRow(cells.Select(text => (IElement)new TextBlock(text)).ToArray());
    }

    /// <summary>Adds a row of strings and elements. Other values are rejected before adding the row.</summary>
    public void AddRow(params object[] cells) => AddRow(NormaliseCells(cells));

    /// <summary>Adds an empty row.</summary>
    public void AddHeader() => AddHeader(Array.Empty<IElement>());

    /// <summary>Adds a row of text cells.</summary>
    public void AddHeader(params string[] cells) => AddHeader((IEnumerable<string>)cells);

    /// <summary>Adds a row of text cells.</summary>
    public void AddHeader(IEnumerable<string> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        AddHeader(cells.Select(text => (IElement)new TextBlock(text)).ToArray());
    }

    /// <summary>Adds a row of strings and elements. Other values are rejected before adding the row.</summary>
    public void AddHeader(params object[] cells) => AddHeader(NormaliseCells(cells));

    /// <summary>Adds an empty row.</summary>
    public void AddFooter() => AddFooter(Array.Empty<IElement>());

    /// <summary>Adds a row of text cells.</summary>
    public void AddFooter(params string[] cells) => AddFooter((IEnumerable<string>)cells);

    /// <summary>Adds a row of text cells.</summary>
    public void AddFooter(IEnumerable<string> cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        AddFooter(cells.Select(text => (IElement)new TextBlock(text)).ToArray());
    }

    /// <summary>Adds a row of strings and elements. Other values are rejected before adding the row.</summary>
    public void AddFooter(params object[] cells) => AddFooter(NormaliseCells(cells));

    private static IElement[] NormaliseCells(object[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        return Array.ConvertAll(cells, cell => cell switch
        {
            string text => (IElement)new TextBlock(text),
            IElement element => element,
            null => throw new ArgumentNullException(nameof(cells)),
            _ => throw new ArgumentException("Each cell must be a string or an IElement.", nameof(cells))
        });
    }

    /// <summary>
    /// Adds a header row to the table
    /// </summary>
    /// <example>
    /// <code>
    /// table.AddHeader(new TextBlock("Item"), new TextBlock("Qty"), new TextBlock("Price"));
    /// </code>
    /// </example>
    public void AddHeader(params IElement[] cells)
    {
        TableRow row = TableRow.From(cells).AsHeader();
        _headers.Add(row);
    }

    /// <summary>
    /// Adds a header row to the table with custom cells
    /// </summary>
    public void AddHeader(params TableCell[] cells)
    {
        foreach (var c in cells) c.IsHeader = true;
        TableRow row = TableRow.From(cells).AsHeader();
        _headers.Add(row);
    }

    /// <summary>
    /// Adds a header row to the table from a TableRow
    /// </summary>
    public void AddHeader(TableRow row)
    {
        row.IsHeader = true;
        // Propagate to cells so each cell's PDF/UA role resolves to TH
        // when the structure tree is built.
        foreach (var c in row.Cells) c.IsHeader = true;
        _headers.Add(row);
    }

    /// <summary>
    /// Adds a footer row to the table
    /// </summary>
    /// <example>
    /// <code>
    /// table.AddFooter(new TextBlock("Total"), new TextBlock("4"), new TextBlock("£48.00"));
    /// </code>
    /// </example>
    public void AddFooter(params IElement[] cells)
    {
        TableRow row = TableRow.From(cells).AsFooter();
        _footers.Add(row);
    }

    /// <summary>
    /// Adds a footer row to the table with custom cells
    /// </summary>
    public void AddFooter(params TableCell[] cells)
    {
        TableRow row = TableRow.From(cells).AsFooter();
        _footers.Add(row);
    }

    /// <summary>
    /// Adds a footer row to the table from a TableRow
    /// </summary>
    public void AddFooter(TableRow row)
    {
        row.IsFooter = true;
        _footers.Add(row);
    }

    /// <summary>
    /// Adds a data row to the table
    /// Accepts IElement elements (including TableCell which implements IElement)
    /// </summary>
    /// <example>
    /// <code>
    /// table.AddRow(new TextBlock("Widget"), new TextBlock("4"), new TextBlock("£12.00"));
    /// </code>
    /// </example>
    public void AddRow(params IElement[] cells)
    {
        ArgumentNullException.ThrowIfNull(cells);
        // A row of plain content gets default cells, so it is kept as the content alone
        // (TableRowStore); a TableCell among the elements is the caller's and is kept as given.
        if (Array.TrueForAll(cells, cell => cell is not TableCell)) _rows.AddContents(cells);
        else _rows.Add(TableRow.From(cells));
    }

    /// <summary><see cref="AddRow(IElement[])"/> with one element, without the array.</summary>
    internal void AddRow(IElement cell)
    {
        if (cell is TableCell) _rows.Add(TableRow.From(cell));
        else _rows.AddContent(cell);
    }

    /// <summary>
    /// Adds a data row to the table from a TableRow
    /// </summary>
    public void AddRow(TableRow row)
    {
        _rows.Add(row);
    }

    /// <summary>
    /// Renders this table with state tracking for pagination.
    /// Rows that don't fit on the current page will be continued on the next page.
    /// Headers repeat on each continuation page if RepeatHeadersOnNewPage is true.
    /// </summary>
    public RenderStatus RenderStateful(RenderContext ctx, float width)
    {
        // Apply top margin only on first render
        if (_lastRowRendered < 0 && _headersPending) ctx.CurrentY += Margin.Top;
        float originalX = ctx.CurrentX;
        ctx.CurrentX += Margin.Left;

        TaggingContext? tagging = ctx.Tagging;
        // Guarantee a Table ancestor for the row-groups. The pipeline/stack opens
        // the Table for the first fragment, but a continuation fragment after a
        // page break re-enters with only the page/Document open - so open a Table
        // here when one isn't already current (PDF/UA 7.2). Guard avoids
        // double-wrapping the common (already-wrapped) case. A paginated table can
        // yield one Table per page fragment, which is structurally valid.
        bool ownTable = tagging != null && tagging.Tree.Current.Role != GroupRole;
        if (ownTable) tagging!.Tree.Begin(GroupRole);
        try
        {
            return RenderStatefulRows(ctx, width, tagging);
        }
        finally
        {
            // Each call draws one page's fragment: the last row's bottom edges belong to this page.
            FlushBottomEdges(ctx);
            ctx.CurrentX = originalX;
            if (ownTable) tagging!.Tree.End();
        }
    }

    private RenderStatus RenderStatefulRows(RenderContext ctx, float width, TaggingContext? tagging)
    {
        float contentWidth = GetContentWidth(width);
        float[] widths = PaginatedColumnWidths(contentWidth);
        // Layout (presentation) tables emit no TR/TH/TD and therefore no row-groups.
        TaggingContext? groupTagging = IsLayoutGrid ? null : tagging;
        // PDF/UA 7.2: rows must sit inside a THead/TBody/TFoot row-group. Groups
        // are opened lazily (only once a row actually renders, so no empty groups)
        // and closed before every return so the structure tree stays balanced.

        // Render headers if pending (first page or continuation with RepeatHeadersOnNewPage).
        // On the first page the header rows are tagged as THead (structural).
        // Continuation headers use TBody so the table has exactly one THead,
        // and their tagged cells remain outside artifact regions.
        if (_headersPending)
        {
            bool isContinuation = _lastRowRendered >= 0;
            bool theadOpen = false;

            foreach (TableRow headerRow in _headers)
            {
                float headerHeight = MeasureRowHeight(headerRow, widths);

                if (ctx.CurrentY + headerHeight > ctx.MaxY && ctx.CurrentY > ctx.ContentTop)
                {
                    if (theadOpen && groupTagging != null) groupTagging!.Tree.End();
                    return _lastRowRendered < 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                }

                if (!theadOpen)
                {
                    if (groupTagging != null)
                    {
                        groupTagging.Tree.Begin(isContinuation ? SemanticRole.TBody : SemanticRole.THead);
                        theadOpen = true;
                    }
                }

                RenderRowDirect(ctx, headerRow, widths);
            }

            if (theadOpen && groupTagging != null) groupTagging!.Tree.End();
            _headersPending = false;
        }

        // Start from the next unrendered row
        int startRow = _lastRowRendered + 1;

        if (startRow >= _rows.Count)
        {
            RenderFootersGrouped(ctx, widths, tagging);
            ctx.CurrentY += Margin.Bottom;
            return RenderStatus.Complete;
        }

        // Render data rows
        bool tbodyOpen = false;
        bool running = RunningTotalColumn.HasValue;
        for (int i = startRow; i < _rows.Count; i++)
        {
            TableRow row = _rows.ForLayout(i, reuse: ctx.LayoutRecorder is null);
            float rowHeight = MeasureRowHeight(row, widths);
            // A running total opens a continuation page with a brought-forward row, and needs room after
            // every row but the last for the carried-forward row that may end the page.
            TableRow? broughtForward = running && _carryPending ? RunningRow(BroughtForwardLabel) : null;
            float broughtForwardHeight = broughtForward is null ? 0 : MeasureRowHeight(broughtForward, widths);
            float carryHeight = running && i < _rows.Count - 1 ? MeasureRowHeight(RunningRow(CarriedForwardLabel), widths) : 0;

            // Pagination check
            if (ctx.CurrentY + broughtForwardHeight + rowHeight + carryHeight > ctx.MaxY && ctx.CurrentY > ctx.ContentTop)
            {
                if (tbodyOpen && groupTagging != null) groupTagging!.Tree.End();
                if (running && i > startRow)
                {
                    RenderRowDirect(ctx, RunningRow(CarriedForwardLabel), widths, asArtifact: true);
                    _carryPending = true;
                }
                _lastRowRendered = i - 1;
                _headersPending = RepeatHeadersOnNewPage && _headers.Count > 0;
                return i == startRow ? RenderStatus.WrapRequired : RenderStatus.Overflow;
            }

            if (broughtForward is not null)
            {
                RenderRowDirect(ctx, broughtForward, widths, asArtifact: true);
                _carryPending = false;
            }
            if (!tbodyOpen && groupTagging != null) { groupTagging.Tree.Begin(SemanticRole.TBody); tbodyOpen = true; }
            RenderRowDirect(ctx, row, widths);
            if (running) _runningValues.Add(row.RunningValue ?? 0m);
            _lastRowRendered = i;
        }
        if (tbodyOpen && groupTagging != null) groupTagging!.Tree.End();

        RenderFootersGrouped(ctx, widths, tagging);
        ctx.CurrentY += Margin.Bottom;
        _lastRowRendered = _rows.Count - 1;
        return RenderStatus.Complete;
    }

    /// <summary>Renders footer rows wrapped in a TFoot row-group (PDF/UA 7.2).</summary>
    private void RenderFootersGrouped(RenderContext ctx, float[] widths, TaggingContext? tagging)
    {
        if (_footers.Count == 0) return;
        TaggingContext? groupTagging = IsLayoutGrid ? null : tagging;
        if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.TFoot);
        foreach (TableRow footerRow in _footers)
            RenderRowDirect(ctx, footerRow, widths);
        if (groupTagging != null) groupTagging!.Tree.End();
    }

    /// <summary>
    /// Resets the internal state, preparing the table for a fresh render.
    /// </summary>
    public void ResetState()
    {
        _lastRowRendered = -1;
        _headersPending = true;
        _runningValues.Clear();
        _carryPending = false;
        _bottomsAbove = null;
        _edgePlan.Clear();
        _pendingBottoms.Clear();
        _paginatedWidths = null;
    }

    // --- Border collapse ---
    //
    // Two adjacent cells share an edge: the right edge of the left cell is the left edge of the right cell,
    // and the bottom edge of a cell is the top edge of the cell below it. Each shared edge is drawn once.
    // The cell earlier in reading order (the left one, the upper one) always draws it; the later cell draws
    // it only when its own border is more prominent, and so paints over the earlier one: the wider line
    // wins, then the darker, and a tie goes to the earlier cell.

    private readonly Dictionary<TableCell, (bool DrawLeft, bool DrawTop)> _edgePlan = new();

    // The bottom edges of the row last drawn, held until the next row's fills are down (see PaintCellFill).
    private readonly List<(float X, float Y, float Width, float LineWidth, Color Color)> _pendingBottoms = new();
    private int _pendingBottomsPage = -1;

    // The bottom edge of each column in the row above, on the page being drawn; null when there is none.
    private (float Width, Color? Color)[]? _bottomsAbove;
    private int _bottomsAbovePage = -1;

    // A cell that sets any border of its own uses it; every other cell takes the table's. (A cell's border is
    // never null, so falling back only when it is unset would never use the table's at all.)
    private (Border Border, BorderColors Colors) EdgesOf(TableCell cell) =>
        cell.Border.HasAnyBorder ? (cell.Border, cell.BorderColors) : (Border, BorderColors);

    /// <summary>The width a side actually draws at: nothing when it has no colour.</summary>
    private static float Drawn(float width, Color? color) => color.HasValue ? width : 0;

    /// <summary>True when the first edge is strictly more prominent than the second.</summary>
    private static bool Outranks(float widthA, Color? colorA, float widthB, Color? colorB)
    {
        float a = Drawn(widthA, colorA);
        float b = Drawn(widthB, colorB);
        if (a != b) return a > b;
        if (a == 0 || !colorA.HasValue || !colorB.HasValue) return false;
        return Luminance(colorA.Value) < Luminance(colorB.Value);
    }

    private static float Luminance(Color c) => 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;

    private void PlanRowEdges(RenderContext ctx, TableRow row)
    {
        _edgePlan.Clear();
        bool aboveKnown = _bottomsAbove != null && _bottomsAbovePage == ctx.CurrentPageIndex;

        (Border Border, BorderColors Colors)? previous = null;
        int col = 0;
        foreach (TableCell cell in row.Cells)
        {
            (Border border, BorderColors colors) = EdgesOf(cell);
            int span = Math.Max(1, cell.ColumnSpan);

            bool drawLeft = previous is not { } left ||
                Outranks(border.Left, colors.Left, left.Border.Right, left.Colors.Right);

            bool drawTop = true;
            if (aboveKnown)
            {
                // Against the most prominent bottom edge among the columns this cell covers.
                (float Width, Color? Color) strongest = default;
                for (int j = 0; j < span && col + j < _bottomsAbove!.Length; j++)
                {
                    (float Width, Color? Color) above = _bottomsAbove[col + j];
                    if (Outranks(above.Width, above.Color, strongest.Width, strongest.Color)) strongest = above;
                }

                drawTop = Outranks(border.Top, colors.Top, strongest.Width, strongest.Color);
            }

            _edgePlan[cell] = (drawLeft, drawTop);
            previous = (border, colors);
            col += span;
        }
    }

    private void RecordRowBottoms(RenderContext ctx, TableRow row, float[] widths)
    {
        // The row above's array is read only while planning this row's edges, so it is refilled in place.
        var bottoms = _bottomsAbove is { } reused && reused.Length == widths.Length ? reused : new (float Width, Color? Color)[widths.Length];
        Array.Clear(bottoms);
        int col = 0;
        foreach (TableCell cell in row.Cells)
        {
            (Border border, BorderColors colors) = EdgesOf(cell);
            int span = Math.Max(1, cell.ColumnSpan);
            for (int j = 0; j < span && col + j < bottoms.Length; j++)
                bottoms[col + j] = (Drawn(border.Bottom, colors.Bottom), colors.Bottom);
            col += span;
        }

        _bottomsAbove = bottoms;
        _bottomsAbovePage = ctx.CurrentPageIndex;
    }

    /// <summary>
    /// Renders a row without internal pagination checks.
    /// Used by RenderStateful() where pagination is handled externally.
    ///
    /// Tagging: pushes /TR around the row and /TH or /TD around
    /// each cell when the document has opted into PDF/UA. ColumnSpan
    /// surfaces as /Attributes /ColSpan on the cell's StructElem.
    /// No-op when tagging is off; see RenderRow for the non-stateful
    /// twin that lives alongside this method for the
    /// non-pipeline render path.
    /// </summary>
    /// <summary>
    /// Records the row + per-cell nodes into the layout snapshot so the structure
    /// and compliance views see the real <c>Table → TableRow → TableCell</c>
    /// (TR/TH/TD) hierarchy — mirroring what the tagging pipeline writes to the PDF
    /// structure tree. A no-op without a recorder, so PDF/SVG output is unchanged.
    /// The cell's intrinsic <c>SemanticRole</c> (TH/TD) is read by the recorder's
    /// a11y capture, so a table with header cells is correctly seen as having
    /// headers. Returns callbacks to invoke around each cell and after the row.
    /// </summary>
    private (System.Action<TableCell, float, float> OpenCell, System.Action<TableCell> CloseCell, System.Action CloseRow)
        CaptureRow(RenderContext ctx, TableRow row, float rowX, float rowY, float rowWidth, float rowHeight)
    {
        // Presentation tables emit no TR/TH/TD structure, so we don't record
        // row/cell nodes either — the cell contents (tagged in reading order) nest
        // directly under the table's Div node, mirroring the structure tree.
        LayoutRecorder? rec = IsLayoutGrid ? null : ctx.LayoutRecorder;
        // Without a recorder (every PDF save), share no-op callbacks instead of allocating three per row.
        return rec is null ? NoRowCapture : CaptureRow(rec, row, ctx.CurrentPageIndex, rowX, rowY, rowWidth, rowY + rowHeight);
    }

    private static readonly (System.Action<TableCell, float, float> OpenCell, System.Action<TableCell> CloseCell, System.Action CloseRow)
        NoRowCapture = (static (_, _, _) => { }, static _ => { }, static () => { });

    private static (System.Action<TableCell, float, float> OpenCell, System.Action<TableCell> CloseCell, System.Action CloseRow)
        CaptureRow(LayoutRecorder rec, TableRow row, int page, float rowX, float rowY, float rowWidth, float endY)
    {
        rec.OpenNode(row, LayoutRecorder.CaptureTypeOf(row), LayoutRecorder.CaptureNameOf(row), page, rowX, rowY, rowWidth);
        return (
            (cell, x, width) => rec.OpenNode(cell, LayoutRecorder.CaptureTypeOf(cell), LayoutRecorder.CaptureNameOf(cell), page, x, rowY, width),
            cell => rec.CloseNode(cell, page, endY, true),
            () => rec.CloseNode(row, page, endY, true)
        );
    }

    /// <summary>
    /// Renders a row without pagination checks. Shared body for RenderRow,
    /// which adds the footer-visibility and pagination checks first.
    /// Tagging: pushes a /TR struct around the row and a /TH or /TD
    /// around each cell so the structure tree mirrors the visible table.
    /// ColumnSpan surfaces as /Attributes /ColSpan on the cell's StructElem;
    /// header-ness is inherited from the row when the cell doesn't already
    /// declare it. Renders unchanged (and adds nothing to the content
    /// stream) when tagging is off.
    /// </summary>
    private void RenderRowDirect(RenderContext ctx, TableRow row, float[] widths, bool asArtifact = false)
    {
        // A carried- or brought-forward row repeats what the rows already say: draw it as an artifact.
        bool artifact = asArtifact && ctx.Tagging != null && !ctx.Canvas.IsInsideArtifact();
        if (artifact) ctx.Canvas.BeginArtifact();
        try
        {
            RenderRowContent(ctx, row, widths, artifact);
        }
        finally
        {
            if (artifact) ctx.Canvas.EndArtifact();
        }
    }

    /// <summary>
    /// The carried-forward or brought-forward row: the label across the columns before
    /// <see cref="RunningTotalColumn"/>, the running total formatted in that column, and empty cells after it.
    /// </summary>
    private TableRow RunningRow(string label)
    {
        int column = RunningTotalColumn!.Value;
        int columns = ColumnCount();
        if (column < 1 || column >= columns)
            throw new ArgumentOutOfRangeException(nameof(RunningTotalColumn), "RunningTotalColumn must be a column index from 1 to the last column.");
        decimal sum = 0m;
        foreach (decimal value in _runningValues) sum += value;
        string total = RunningTotalPrefix + ChartNumberFormat.Format(sum, RunningTotalFormat);
        TextBlock labelText = new(label) { FontWeight = FontWeight.Bold };
        TextBlock valueText = new(total) { FontWeight = FontWeight.Bold, Alignment = TextAlignment.Right };
        if (CascadedTextStyle != null)
        {
            labelText.ApplyDefaultStyleIfNotSet(CascadedTextStyle);
            valueText.ApplyDefaultStyleIfNotSet(CascadedTextStyle);
        }
        var cells = new List<TableCell>
        {
            new() { Content = labelText, ColumnSpan = column },
            new() { Content = valueText },
        };
        for (int k = column + 1; k < columns; k++) cells.Add(new TableCell { Content = new TextBlock("") });
        var row = new TableRow { Cells = [.. cells] };
        if (FooterBackgroundColor.HasValue) row.BackgroundColor = FooterBackgroundColor;
        if (FooterTextColor.HasValue) row.Color = FooterTextColor;
        return row;
    }

    private void RenderRowContent(RenderContext ctx, TableRow row, float[] widths, bool suppressTagging)
    {
        float rowHeight = MeasureRowHeight(row, widths);
        PlanRowEdges(ctx, row);

        var tagging = suppressTagging ? null : ctx.Tagging;
        // Data tables tag TR/TH/TD; presentation (layout) tables tag each cell's
        // content in reading order instead (no table structure). Exactly one applies.
        bool dataTagging = tagging != null && !IsLayoutGrid;
        bool presTagging = tagging != null && IsLayoutGrid;
        if (dataTagging)
        {
            tagging!.Tree.Begin(SemanticRole.TR);
            ctx.Canvas.BeginContainerContent("TR");
        }

        var capture = CaptureRow(ctx, row, ctx.CurrentX, ctx.CurrentY, widths.Sum(), rowHeight);

        try
        {
            float startX = ctx.CurrentX;
            // Each cell's left edge and width. `col` tracks the column the cell starts in, which runs ahead
            // of the cell index once any cell in the row spans more than one column.
            var cellX = new float[row.Cells.Length];
            var cellWidth = new float[row.Cells.Length];
            float currentX = startX + (IsRightToLeft ? widths.Sum() : 0);
            for (int i = 0, col = 0; i < row.Cells.Length; i++)
            {
                int span = Math.Max(1, row.Cells[i].ColumnSpan);
                float colWidth = 0;
                for (int j = 0; j < span && col + j < widths.Length; j++)
                    colWidth += widths[col + j];
                if (IsRightToLeft) currentX -= colWidth;
                cellX[i] = currentX;
                cellWidth[i] = colWidth;
                if (!IsRightToLeft) currentX += colWidth;
                col += span;
            }

            // Paint order: the row's fills, then the edges held back from the row above, then the cells'
            // content, then the row's own edges. No fill is painted over an edge.
            for (int i = 0; i < row.Cells.Length; i++)
                PaintCellFill(ctx, row.Cells[i], row, cellX[i], cellWidth[i], rowHeight);
            FlushBottomEdges(ctx);

            for (int i = 0; i < row.Cells.Length; i++)
            {
                TableCell cell = row.Cells[i];

                // Defensive: cells added via fromElements don't know their
                // row's header-ness. Propagate at render time so the role
                // resolves to TH instead of TD.
                if (row.IsHeader && !cell.IsHeader) cell.IsHeader = true;

                float colWidth = cellWidth[i];
                currentX = cellX[i];
                capture.OpenCell(cell, currentX, colWidth);

                ChildTagToken childTag = default;
                if (dataTagging)
                {
                    SemanticRole cellRole = cell.SemanticRole;
                    tagging!.Tree.Begin(cellRole);
                    var cellAttrs = new List<string>();
                    if (cell.ColumnSpan > 1) cellAttrs.Add($"/ColSpan {cell.ColumnSpan}");
                    // PDF/UA 7.5: TH cells need a Scope. Header rows are column headers.
                    if (cellRole == SemanticRole.TH) cellAttrs.Add("/Scope /Column");
                    if (cellAttrs.Count > 0)
                        tagging!.Tree.SetAttributes($"<< /O /Table {string.Join(" ", cellAttrs)} >>");
                    ctx.Canvas.BeginMarkedContent(cellRole.ToPdfTag());
                }
                else if (presTagging)
                {
                    // Layout cell: tag the content with its own natural role (P,
                    // Figure, …) in reading order, exactly as a stack tags a child.
                    childTag = ChildTagging.BeginChildTag(ctx, cell.Content);
                }

                try
                {
                    RenderCell(ctx, cell, row, currentX, colWidth, rowHeight);
                }
                finally
                {
                    if (dataTagging)
                    {
                        ctx.Canvas.EndMarkedContent();
                        tagging!.Tree.End();
                    }
                    else if (presTagging)
                    {
                        ChildTagging.EndChildTag(ctx, childTag);
                    }
                    capture.CloseCell(cell);
                }
            }

            for (int i = 0; i < row.Cells.Length; i++) DrawCellEdges(ctx, row.Cells[i], cellX[i], cellWidth[i], rowHeight);

            ctx.CurrentX = startX;
            RecordRowBottoms(ctx, row, widths);
            ctx.CurrentY += rowHeight;
        }
        finally
        {
            if (dataTagging)
            {
                ctx.Canvas.EndMarkedContent();
                tagging!.Tree.End();
            }
            capture.CloseRow();
        }
    }

    protected override float MeasureContentHeight(float contentWidth)
    {
        float totalHeight = 0;
        float[] widths = CalculateColumnWidths(contentWidth);

        // Headers
        foreach (TableRow row in _headers) totalHeight += MeasureRowHeight(row, widths);

        // Data rows
        for (int i = 0; i < _rows.Count; i++) totalHeight += MeasureRowHeight(_rows.ForLayout(i, reuse: true), widths);

        // Footers
        foreach (TableRow row in _footers) totalHeight += MeasureRowHeight(row, widths);

        return totalHeight;
    }

    protected override void RenderContent(RenderContext ctx, float contentWidth)
    {
        TaggingContext? tagging = ctx.Tagging;
        // Some hosts (e.g. VerticalStack) already open a Table struct element for
        // IRoleProvider children before calling Render. Only open our own when one
        // isn't already current, so the THead/TBody/TFoot groups get exactly one
        // Table ancestor (PDF/UA 7.2) instead of Table-in-Table.
        bool ownTable = tagging != null && tagging.Tree.Current.Role != GroupRole;
        if (ownTable) tagging!.Tree.Begin(GroupRole);
        try
        {
            RenderContentRows(ctx, contentWidth, tagging);
        }
        finally
        {
            FlushBottomEdges(ctx);
            if (ownTable) tagging!.Tree.End();
        }
    }

    private void RenderContentRows(RenderContext ctx, float contentWidth, TaggingContext? tagging)
    {
        float[] widths = CalculateColumnWidths(contentWidth);
        // Layout (presentation) tables emit no TR/TH/TD and therefore no row-groups.
        TaggingContext? groupTagging = IsLayoutGrid ? null : tagging;

        // Render headers - grouped under THead (PDF/UA 7.2).
        if (_headers.Count > 0)
        {
            if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.THead);
            try
            {
                foreach (TableRow row in _headers) RenderRow(ctx, row, widths);
            }
            finally
            {
                if (groupTagging != null) groupTagging!.Tree.End();
            }
        }

        // Render data rows - grouped under TBody.
        if (_rows.Count > 0)
        {
            if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.TBody);
            try
            {
                for (int i = 0; i < _rows.Count; i++)
                {
                    TableRow row = _rows.ForLayout(i, reuse: ctx.LayoutRecorder is null);
                    float rowHeight = MeasureRowHeight(row, widths);

                    // Pagination check
                    if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.ContentTop)
                    {
                        FlushBottomEdges(ctx);
                        ctx.NewPage();

                        // Repeat headers on new page if enabled
                        if (RepeatHeadersOnNewPage && _headers.Count > 0)
                        {
                            foreach (TableRow headerRow in _headers) RenderRow(ctx, headerRow, widths);
                        }
                    }

                    RenderRow(ctx, row, widths);
                }
            }
            finally
            {
                if (groupTagging != null) groupTagging!.Tree.End();
            }
        }

        // Render footers - grouped under TFoot.
        if (_footers.Count > 0)
        {
            if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.TFoot);
            try
            {
                foreach (TableRow row in _footers) RenderRow(ctx, row, widths);
            }
            finally
            {
                if (groupTagging != null) groupTagging!.Tree.End();
            }
        }
    }

    private void RenderRow(RenderContext ctx, TableRow row, float[] widths)
    {
        float rowHeight = MeasureRowHeight(row, widths);

        // Pagination check
        if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.ContentTop)
        {
            FlushBottomEdges(ctx);
            ctx.NewPage();
        }

        RenderRowDirect(ctx, row, widths);
    }

    private void RenderCell(RenderContext ctx, TableCell cell, TableRow row, float x, float width, float rowHeight)
    {
        // The fill cascade is the only place a cell's effective fill is known —
        // the cell element itself carries no BackgroundColor when the colour came
        // from the row or the table. Hand it to the recorder so the contrast rules
        // measure the cell's text against its real backdrop, not the page.
        Color? bgColor = CellFill(cell, row);
        if (bgColor.HasValue)
            ctx.LayoutRecorder?.AnnotatePaint(cell, LayoutRecorder.PaintForBackground(bgColor.Value));

        // Determine effective styling (cell > row > table defaults)
        Color? color = cell.Color
                           ?? row.Color
                           ?? (row.IsHeader ? HeaderTextColor : null)
                           ?? (row.IsFooter ? FooterTextColor : null)
                           ?? RowTextColor;

        Thickness padding = cell.Padding ?? row.Padding ?? new Thickness(CellPadding);

        // Apply text color to TextBlock content if specified
        IElement content = cell.Content;
        if (color.HasValue && content is TextBlock tb)
        {
            // Create a copy with the text color applied
            tb = new TextBlock
            {
                Text = tb.Text,
                FontName = tb.FontName,
                FontSize = tb.FontSize,
                Color = color.Value
            };
            var source = (TextBlock)cell.Content;
            if (source.AlignmentIsExplicit) tb.Alignment = source.Alignment;
            ContentDirectionResolver.Apply(tb, source.EffectiveContentDirection);
            content = tb;
        }

        // Render content with padding - IMPORTANT: Save Y before and restore after
        // to prevent cells from advancing Y (they should all be on the same row)
        float originalX = ctx.CurrentX;
        float yBeforeCell = ctx.CurrentY;
        ctx.CurrentX = x + padding.Left;
        ctx.CurrentY += padding.Top;

        float contentWidth = width - padding.Left - padding.Right;
        ctx.CellStretchTarget = StretchableContainer(content);
        ctx.CellStretchHeight = rowHeight - padding.Top - padding.Bottom;
        try
        {
            content.Render(ctx, contentWidth);
        }
        finally
        {
            ctx.CellStretchTarget = null;
        }

        // Restore X and Y positions
        ctx.CurrentX = originalX;
        ctx.CurrentY = yBeforeCell; // Restore Y so next cell starts at same position

    }

    /// <summary>
    /// Paints a cell's fill. Every fill in a row is painted before any of the row's edges, and a row's
    /// bottom edges wait for the next row's fills, so no fill covers an edge (spec: Paint order).
    /// </summary>
    private void PaintCellFill(RenderContext ctx, TableCell cell, TableRow row, float x, float width, float rowHeight)
    {
        Color? bgColor = CellFill(cell, row);
        if (bgColor.HasValue) DrawBackground(ctx, x, ctx.CurrentY, width, rowHeight, bgColor.Value);
    }

    /// <summary>A cell's effective fill: cell, then row, then the table's header, footer or row default.</summary>
    private Color? CellFill(TableCell cell, TableRow row) =>
        cell.BackgroundColor
        ?? row.BackgroundColor
        ?? (row.IsHeader ? HeaderBackgroundColor : null)
        ?? (row.IsFooter ? FooterBackgroundColor : null)
        ?? RowBackgroundColor;

    /// <summary>
    /// Draws a cell's left, top and right edges, and holds its bottom edge for <see cref="FlushBottomEdges"/>.
    /// A rounded cell draws its whole outline at once.
    /// </summary>
    private void DrawCellEdges(RenderContext ctx, TableCell cell, float x, float width, float rowHeight)
    {
        (Border edges, BorderColors colors) = EdgesOf(cell);
        if (!edges.HasAnyBorder) return;
        // A shared edge is drawn once, by the cell that wins it (PlanRowEdges).
        bool planned = _edgePlan.TryGetValue(cell, out (bool DrawLeft, bool DrawTop) plan);
        bool rounded = CornerRadius > 0;
        float left = planned && !plan.DrawLeft ? 0 : edges.Left;
        float top = planned && !plan.DrawTop ? 0 : edges.Top;
        if (!rounded && edges.Bottom > 0 && colors.Bottom.HasValue)
        {
            _pendingBottomsPage = ctx.CurrentPageIndex;
            _pendingBottoms.Add((x, ctx.CurrentY + rowHeight, width, edges.Bottom, colors.Bottom.Value));
        }
        DrawBorder(ctx, x, ctx.CurrentY, width, rowHeight, new Border(left, top, edges.Right, rounded ? edges.Bottom : 0), colors);
    }

    /// <summary>
    /// Draws the bottom edges held back from the row above, once the row below has painted its fills, or
    /// when the table leaves the page. Edges held on another page are already drawn there.
    /// </summary>
    private void FlushBottomEdges(RenderContext ctx)
    {
        if (_pendingBottoms.Count == 0) return;
        bool samePage = _pendingBottomsPage == ctx.CurrentPageIndex;
        if (samePage)
        {
            ctx.Canvas.BeginArtifact();
            try
            {
                foreach (var edge in _pendingBottoms)
                    DrawLine(ctx, edge.X, edge.Y, edge.X + edge.Width, edge.Y, edge.LineWidth, edge.Color);
            }
            finally
            {
                ctx.Canvas.EndArtifact();
            }
        }
        _pendingBottoms.Clear();
    }

    /// <summary>
    /// The container that is the cell's own content, looking through the wrappers that only pass a child
    /// along (the fluent slot, the pagination primitives), or <c>null</c> when the content is anything else.
    /// </summary>
    private static Container? StretchableContainer(IElement content)
    {
        IElement current = content;
        while (true)
        {
            switch (current)
            {
                case Container container:
                    return container;
                case BonniePDF.Fluent.ContentSlot { Child: not null } slot:
                    current = slot.Child;
                    break;
                case PaginationWrapperBase { Child: not null } wrapper:
                    current = wrapper.Child;
                    break;
                default:
                    return null;
            }
        }
    }

    private float[]? _paginatedWidths;
    private float _paginatedWidthsFor;

    /// <summary>
    /// Column widths for the page fragments of one render. Finding the column count
    /// scans every row, so recomputing it on each page made one table O(rows x pages).
    /// The tree is frozen while it renders; <see cref="ResetState"/> drops the cache.
    /// </summary>
    private float[] PaginatedColumnWidths(float totalWidth)
    {
        if (_paginatedWidths is null || _paginatedWidthsFor != totalWidth)
        {
            _paginatedWidths = CalculateColumnWidths(totalWidth);
            _paginatedWidthsFor = totalWidth;
        }
        return _paginatedWidths;
    }

    private float[] CalculateColumnWidths(float totalWidth)
    {
        float[] weights = EffectiveColumnWeights();
        // Divided in double. The weights rarely sum to a power of two, so the
        // quotient is not exactly representable and a single-precision division
        // lands a fraction of a thousandth away from the TypeScript
        // implementation's - which put a table header band on 56.618 against its
        // 56.619 once the width was rounded for output.
        double totalWeight = weights.Sum(w => (double)w);
        return weights.Select(w => (float)(w / totalWeight * totalWidth)).ToArray();
    }

    /// <summary>
    /// The declared column weights, extended with equal weights out to the widest
    /// row. A table may be built with no weights at all, or with fewer than some
    /// row actually fills; the columns those rows imply share the width evenly
    /// rather than being left without one.
    /// </summary>
    private float[] EffectiveColumnWeights()
    {
        int columns = ColumnCount();
        if (columns <= _columnWeights.Length) return _columnWeights;

        var weights = new float[columns];
        _columnWeights.CopyTo(weights, 0);
        for (int i = _columnWeights.Length; i < columns; i++) weights[i] = 1f;
        return weights;
    }

    /// <summary>
    /// The number of columns the table spans: the declared weight count, widened
    /// to the most columns any single row occupies once column spans are counted.
    /// </summary>
    private int ColumnCount()
    {
        int columns = _columnWeights.Length;

        foreach (List<TableRow> rows in (List<TableRow>[])[_headers, _footers])
        {
            foreach (TableRow row in rows)
            {
                int spanned = 0;
                foreach (TableCell cell in row.Cells) spanned += Math.Max(1, cell.ColumnSpan);
                if (spanned > columns) columns = spanned;
            }
        }
        // Body rows, without materialising compact ones: their default cells each span one column.
        for (int i = 0; i < _rows.Count; i++)
        {
            int spanned = 0;
            if (_rows.IsCompact(i)) spanned = _rows.CellCount(i);
            else foreach (TableCell cell in _rows[i].Cells) spanned += Math.Max(1, cell.ColumnSpan);
            if (spanned > columns) columns = spanned;
        }

        return columns;
    }

    private float MeasureRowHeight(TableRow row, float[] widths)
    {
        float maxHeight = row.MinHeight ?? 0;

        // `col` tracks the column the cell starts in, which runs ahead of the cell's
        // position in the row once any cell in the row spans more than one column.
        int col = 0;

        foreach (TableCell cell in row.Cells)
        {
            Thickness padding = cell.Padding ?? row.Padding ?? new Thickness(CellPadding);

            // Calculate effective width considering column span
            int span = Math.Max(1, cell.ColumnSpan);
            float cellWidth = 0;
            for (int j = 0; j < span && col + j < widths.Length; j++)
                cellWidth += widths[col + j];
            col += span;

            // Measure content height with padding
            float contentWidth = cellWidth - padding.Left - padding.Right;
            float contentHeight = cell.Content.MeasureHeight(contentWidth);
            float totalCellHeight = contentHeight + padding.Top + padding.Bottom;

            if (totalCellHeight > maxHeight) maxHeight = totalCellHeight;
        }

        return maxHeight;
    }

}
