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
    /// <inheritdoc />
    public SemanticRole SemanticRole => Rendering.Tagging.SemanticRole.Table;

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

        float contentWidth = GetContentWidth(width);
        float[] widths = CalculateColumnWidths(contentWidth);

        // Render headers if pending (first page or continuation with RepeatHeadersOnNewPage)
        if (_headersPending)
        {
            foreach (TableRow headerRow in _headers)
            {
                float headerHeight = MeasureRowHeight(headerRow, widths);

                if (ctx.CurrentY + headerHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin)
                {
                    // Header doesn't fit
                    return _lastRowRendered < 0 ? RenderStatus.WrapRequired : RenderStatus.Overflow;
                }

                RenderRowDirect(ctx, headerRow, widths);
            }

            _headersPending = false;
        }

        // Start from the next unrendered row
        int startRow = _lastRowRendered + 1;

        if (startRow >= _rows.Count)
        {
            // All data rows rendered - render footers
            foreach (TableRow footerRow in _footers)
                RenderRowDirect(ctx, footerRow, widths);

            ctx.CurrentY += Margin.Bottom;
            return RenderStatus.Complete;
        }

        // Render data rows
        for (int i = startRow; i < _rows.Count; i++)
        {
            TableRow row = _rows[i];
            float rowHeight = MeasureRowHeight(row, widths);

            // Pagination check
            if (ctx.CurrentY + rowHeight > ctx.MaxY && ctx.CurrentY > ctx.Margin)
            {
                _lastRowRendered = i - 1;
                _headersPending = RepeatHeadersOnNewPage && _headers.Count > 0;
                return i == startRow ? RenderStatus.WrapRequired : RenderStatus.Overflow;
            }

            RenderRowDirect(ctx, row, widths);
            _lastRowRendered = i;
        }

        // All data rows rendered - render footers
        foreach (TableRow footerRow in _footers)
            RenderRowDirect(ctx, footerRow, widths);

        ctx.CurrentY += Margin.Bottom;
        _lastRowRendered = _rows.Count - 1;
        return RenderStatus.Complete;
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
    private void RenderRowDirect(RenderContext ctx, TableRow row, float[] widths)
    {
        float rowHeight = MeasureRowHeight(row, widths);

        var tagging = ctx.Tagging;
        bool tagThisRow = tagging != null;
        if (tagThisRow)
        {
            tagging.Tree.Begin(SemanticRole.TR);
            ctx.Canvas.BeginMarkedContent("TR");
        }

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

                SemanticRole cellRole = cell.SemanticRole;
                bool tagThisCell = tagThisRow;
                if (tagThisCell)
                {
                    tagging.Tree.Begin(cellRole);
                    if (cell.ColumnSpan > 1)
                        tagging.Tree.SetAttributes($"<< /O /Table /ColSpan {cell.ColumnSpan} >>");
                    ctx.Canvas.BeginMarkedContent(cellRole.ToPdfTag());
                }

                try
                {
                    RenderCell(ctx, cell, row, currentX, colWidth, rowHeight);
                }
                finally
                {
                    if (tagThisCell)
                    {
                        ctx.Canvas.EndMarkedContent();
                        tagging.Tree.End();
                    }
                }

                currentX += widths[i];
            }

            ctx.CurrentX = startX;
            ctx.CurrentY += rowHeight;
        }
        finally
        {
            if (tagThisRow)
            {
                ctx.Canvas.EndMarkedContent();
                tagging.Tree.End();
            }
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
        float[] widths = CalculateColumnWidths(contentWidth);
        bool isFirstPage = true;

        // Render headers
        foreach (TableRow row in _headers) RenderRow(ctx, row, widths, isFirstPage);

        // Track if we've added headers on a new page
        bool headersAddedOnNewPage = false;

        // Render data rows
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

        // Render footers
        foreach (TableRow row in _footers) RenderRow(ctx, row, widths, isFirstPage);
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
        bool tagThisRow = tagging != null;
        if (tagThisRow)
        {
            tagging.Tree.Begin(SemanticRole.TR);
            ctx.Canvas.BeginMarkedContent("TR");
        }

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

                SemanticRole cellRole = cell.SemanticRole;
                bool tagThisCell = tagThisRow;
                if (tagThisCell)
                {
                    tagging.Tree.Begin(cellRole);
                    if (cell.ColumnSpan > 1)
                        tagging.Tree.SetAttributes($"<< /O /Table /ColSpan {cell.ColumnSpan} >>");
                    ctx.Canvas.BeginMarkedContent(cellRole.ToPdfTag());
                }

                try
                {
                    // Render cell with proper background
                    RenderCell(ctx, cell, row, currentX, colWidth, rowHeight);
                }
                finally
                {
                    if (tagThisCell)
                    {
                        ctx.Canvas.EndMarkedContent();
                        tagging.Tree.End();
                    }
                }

                currentX += widths[i]; // Move by original column width (not spanned width)
            }

            // Move down by row height
            ctx.CurrentX = startX;
            ctx.CurrentY += rowHeight;
        }
        finally
        {
            if (tagThisRow)
            {
                ctx.Canvas.EndMarkedContent();
                tagging.Tree.End();
            }
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