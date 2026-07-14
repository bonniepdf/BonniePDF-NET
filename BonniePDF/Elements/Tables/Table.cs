using System.Collections.Generic;
using System.Linq;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering;
using BonniePDF.Rendering.Tagging;

namespace BonniePDF.Elements.Tables;

/// <summary>
/// A table element with support for column weights, cell padding, borders, headers, and footers
/// </summary>
public class Table : BorderedElementBase, IStatefulElement, IRoleProvider
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
    private SemanticRole GroupRole => IsLayoutGrid ? Rendering.Tagging.SemanticRole.Div : Rendering.Tagging.SemanticRole.Table;

    /// <inheritdoc />
    public SemanticRole SemanticRole => GroupRole;

    private readonly float[] _columnWeights;
    private readonly List<TableRow> _footers = new();
    private readonly List<TableRow> _headers = new();
    private readonly List<TableRow> _rows = new();

    // State tracking for multipage rendering
    private int _lastRowRendered = -1;
    private bool _headersPending = true;

    public Table(params float[] columnWeights)
    {
        _columnWeights = columnWeights;
    }

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

    /// <inheritdoc /> - every cell's content across headers, body rows, and footers.
    public IEnumerable<IElement> GetChildren() =>
        GetAllRows().SelectMany(row => row.Cells).Where(cell => cell.Content is not null).Select(cell => cell.Content);

    /// <summary>
    /// Adds a header row to the table
    /// </summary>
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
    public void AddRow(params IElement[] cells)
    {
        TableRow row = TableRow.From(cells);
        _rows.Add(row);
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
            ctx.CurrentX = originalX;
            if (ownTable) tagging!.Tree.End();
        }
    }

    private RenderStatus RenderStatefulRows(RenderContext ctx, float width, TaggingContext? tagging)
    {
        float contentWidth = GetContentWidth(width);
        float[] widths = CalculateColumnWidths(contentWidth);
        // Layout (presentation) tables emit no TR/TH/TD and therefore no row-groups.
        TaggingContext? groupTagging = IsLayoutGrid ? null : tagging;
        // PDF/UA 7.2: rows must sit inside a THead/TBody/TFoot row-group. Groups
        // are opened lazily (only once a row actually renders, so no empty groups)
        // and closed before every return so the structure tree stays balanced.

        // Render headers if pending (first page or continuation with RepeatHeadersOnNewPage).
        // On the first page the header rows are tagged as THead (structural).
        // On continuation pages the repeated rows are purely presentational, so they are
        // wrapped in /Artifact to avoid a second THead child under the same Table element
        // (PDF/UA 7.2-11: a Table shall contain zero or one THead).
        if (_headersPending)
        {
            bool isContinuation = _lastRowRendered >= 0;
            bool theadOpen = false;
            bool artifactOpen = false;

            foreach (TableRow headerRow in _headers)
            {
                float headerHeight = MeasureRowHeight(headerRow, widths);

                if (ctx.CurrentY + headerHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin)
                {
                    if (theadOpen && groupTagging != null) groupTagging!.Tree.End();
                    if (artifactOpen) ctx.Canvas.EndArtifact();
                    return _lastRowRendered < 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                }

                if (!theadOpen && !artifactOpen)
                {
                    if (isContinuation && groupTagging != null)
                    {
                        ctx.Canvas.BeginArtifact();
                        artifactOpen = true;
                    }
                    else if (groupTagging != null)
                    {
                        groupTagging.Tree.Begin(SemanticRole.THead);
                        theadOpen = true;
                    }
                }

                RenderRowDirect(ctx, headerRow, widths);
            }

            if (theadOpen && groupTagging != null) groupTagging!.Tree.End();
            if (artifactOpen) ctx.Canvas.EndArtifact();
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
        for (int i = startRow; i < _rows.Count; i++)
        {
            TableRow row = _rows[i];
            float rowHeight = MeasureRowHeight(row, widths);

            // Pagination check
            if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin)
            {
                if (tbodyOpen && groupTagging != null) groupTagging!.Tree.End();
                _lastRowRendered = i - 1;
                _headersPending = RepeatHeadersOnNewPage && _headers.Count > 0;
                return i == startRow ? RenderStatus.WrapRequired : RenderStatus.Overflow;
            }

            if (!tbodyOpen && groupTagging != null) { groupTagging.Tree.Begin(SemanticRole.TBody); tbodyOpen = true; }
            RenderRowDirect(ctx, row, widths);
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
    }

    /// <summary>
    /// Renders a row without internal pagination checks.
    /// Used by RenderStateful() where pagination is handled externally.
    ///
    /// this feature tagging: pushes /TR around the row and /TH or /TD around
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
        int page = ctx.CurrentPageIndex;
        float endY = rowY + rowHeight;
        rec?.OpenNode(row, LayoutRecorder.CaptureTypeOf(row), LayoutRecorder.CaptureNameOf(row), page, rowX, rowY, rowWidth);
        return (
            (cell, x, width) => rec?.OpenNode(cell, LayoutRecorder.CaptureTypeOf(cell), LayoutRecorder.CaptureNameOf(cell), page, x, rowY, width),
            cell => rec?.CloseNode(cell, page, endY, true),
            () => rec?.CloseNode(row, page, endY, true)
        );
    }

    private void RenderRowDirect(RenderContext ctx, TableRow row, float[] widths)
    {
        float rowHeight = MeasureRowHeight(row, widths);

        var tagging = ctx.Tagging;
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
            float currentX = startX;

            for (int i = 0; i < row.Cells.Length; i++)
            {
                TableCell cell = row.Cells[i];

                // Defensive: cells added via fromElements don't know their
                // row's header-ness. Propagate at render time so the role
                // resolves to TH instead of TD.
                if (row.IsHeader && !cell.IsHeader) cell.IsHeader = true;

                float colWidth = widths[i];
                if (cell.ColumnSpan > 1)
                    for (int j = 1; j < cell.ColumnSpan && i + j < widths.Length; j++)
                        colWidth += widths[i + j];

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

                currentX += widths[i];
            }

            ctx.CurrentX = startX;
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
        foreach (TableRow row in _rows) totalHeight += MeasureRowHeight(row, widths);

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
            if (ownTable) tagging!.Tree.End();
        }
    }

    private void RenderContentRows(RenderContext ctx, float contentWidth, TaggingContext? tagging)
    {
        float[] widths = CalculateColumnWidths(contentWidth);
        bool isFirstPage = true;
        // Layout (presentation) tables emit no TR/TH/TD and therefore no row-groups.
        TaggingContext? groupTagging = IsLayoutGrid ? null : tagging;

        // Render headers - grouped under THead (PDF/UA 7.2).
        if (_headers.Count > 0)
        {
            if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.THead);
            try
            {
                foreach (TableRow row in _headers) RenderRow(ctx, row, widths, isFirstPage);
            }
            finally
            {
                if (groupTagging != null) groupTagging!.Tree.End();
            }
        }

        // Track if we've added headers on a new page
        bool headersAddedOnNewPage = false;

        // Render data rows - grouped under TBody.
        if (_rows.Count > 0)
        {
            if (groupTagging != null) groupTagging.Tree.Begin(SemanticRole.TBody);
            try
            {
                foreach (TableRow row in _rows)
                {
                    float rowHeight = MeasureRowHeight(row, widths);

                    // Pagination check
                    if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin)
                    {
                        ctx.NewPage();
                        isFirstPage = false;

                        // Repeat headers on new page if enabled
                        if (RepeatHeadersOnNewPage && _headers.Count > 0)
                        {
                            headersAddedOnNewPage = true;
                            foreach (TableRow headerRow in _headers) RenderRow(ctx, headerRow, widths, false);
                        }
                    }

                    RenderRow(ctx, row, widths, isFirstPage && !headersAddedOnNewPage);
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
                foreach (TableRow row in _footers) RenderRow(ctx, row, widths, isFirstPage);
            }
            finally
            {
                if (groupTagging != null) groupTagging!.Tree.End();
            }
        }
    }

    private void RenderRow(RenderContext ctx, TableRow row, float[] widths, bool isFirstPage)
    {
        // For footers, only show on last page if ShowFootersOnAllPages is false
        if (row.IsFooter && !ShowFootersOnAllPages && !IsLastPage(ctx)) return;

        float rowHeight = MeasureRowHeight(row, widths);

        // Pagination check
        if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin) ctx.NewPage();

        // this feature tagging: push a /TR struct around the row and a /TH or
        // /TD around each cell so the structure tree mirrors the visible
        // table. ColumnSpan surfaces as /Attributes /ColSpan on the
        // cell's StructElem. Header-ness is inherited from the row
        // when the cell doesn't already declare it. Renders unchanged
        // (and adds nothing to the content stream) when tagging is off.
        var tagging = ctx.Tagging;
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
            float currentX = startX;

            for (int i = 0; i < row.Cells.Length; i++)
            {
                TableCell cell = row.Cells[i];

                // Defensive: cells added via TableRow.From() don't know
                // their row's header-ness. Propagate at render time so
                // the structure role resolves to TH instead of TD.
                if (row.IsHeader && !cell.IsHeader) cell.IsHeader = true;

                float colWidth = widths[i];

                // Apply column span
                if (cell.ColumnSpan > 1)
                    for (int j = 1; j < cell.ColumnSpan && i + j < widths.Length; j++)
                        colWidth += widths[i + j];

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
                    childTag = ChildTagging.BeginChildTag(ctx, cell.Content);
                }

                try
                {
                    // Render cell with proper background
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

                currentX += widths[i]; // Move by original column width (not spanned width)
            }

            // Move down by row height
            ctx.CurrentX = startX;
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

    private void RenderCell(RenderContext ctx, TableCell cell, TableRow row, float x, float width, float rowHeight)
    {
        // Determine effective styling (cell > row > table defaults)
        Color? bgColor = cell.BackgroundColor
                         ?? row.BackgroundColor
                         ?? (row.IsHeader ? HeaderBackgroundColor : null)
                         ?? (row.IsFooter ? FooterBackgroundColor : null)
                         ?? RowBackgroundColor;

        Color? textColor = cell.TextColor
                           ?? row.TextColor
                           ?? (row.IsHeader ? HeaderTextColor : null)
                           ?? (row.IsFooter ? FooterTextColor : null)
                           ?? RowTextColor;

        Thickness padding = cell.Padding ?? row.Padding ?? new Thickness(CellPadding);

        Border? cellBorder = cell.Border;
        BorderColors? cellBorderColors = cell.BorderColors;

        // Apply text color to TextBlock content if specified
        IElement content = cell.Content;
        if (textColor.HasValue && content is TextBlock tb)
        {
            // Create a copy with the text color applied
            tb = new TextBlock
            {
                Text = tb.Text,
                FontName = tb.FontName,
                FontSize = tb.FontSize,
                TextColor = textColor.Value,
                Alignment = tb.Alignment
            };
            content = tb;
        }

        // Draw background (fills entire cell including padding area)
        if (bgColor.HasValue) DrawBackground(ctx, x, ctx.CurrentY, width, rowHeight, bgColor.Value);

        // Render content with padding - IMPORTANT: Save Y before and restore after
        // to prevent cells from advancing Y (they should all be on the same row)
        float originalX = ctx.CurrentX;
        float yBeforeCell = ctx.CurrentY;
        ctx.CurrentX = x + padding.Left;
        ctx.CurrentY += padding.Top;

        float contentWidth = width - padding.Left - padding.Right;
        content.Render(ctx, contentWidth);

        // Restore X and Y positions
        ctx.CurrentX = originalX;
        ctx.CurrentY = yBeforeCell; // Restore Y so next cell starts at same position

        // Draw border
        if (cellBorder.HasValue || Border.HasAnyBorder)
        {
            Border effectiveBorder = cellBorder ?? Border;
            BorderColors effectiveBorderColors = cellBorderColors ?? BorderColors;
            DrawBorder(ctx, x, ctx.CurrentY, width, rowHeight, effectiveBorder, effectiveBorderColors);
        }
    }

    private float[] CalculateColumnWidths(float totalWidth)
    {
        float totalWeight = _columnWeights.Sum();
        return _columnWeights.Select(w => w / totalWeight * totalWidth).ToArray();
    }

    private float MeasureRowHeight(TableRow row, float[] widths)
    {
        float maxHeight = row.MinHeight ?? 0;

        for (int i = 0; i < row.Cells.Length; i++)
        {
            TableCell cell = row.Cells[i];
            Thickness padding = cell.Padding ?? row.Padding ?? new Thickness(CellPadding);

            // Calculate effective width considering column span
            float cellWidth = widths[i];
            if (cell.ColumnSpan > 1)
                for (int j = 1; j < cell.ColumnSpan && i + j < widths.Length; j++)
                    cellWidth += widths[i + j];

            // Measure content height with padding
            float contentWidth = cellWidth - padding.Left - padding.Right;
            float contentHeight = cell.Content.MeasureHeight(contentWidth);
            float totalCellHeight = contentHeight + padding.Top + padding.Bottom;

            if (totalCellHeight > maxHeight) maxHeight = totalCellHeight;
        }

        return maxHeight;
    }

    private static bool IsLastPage(RenderContext ctx)
    {
        // Use the RenderContext.IsLastPage property which is set by the render pipeline
        // and TemplateRenderer when processing each page
        return ctx.IsLastPage;
    }
}
