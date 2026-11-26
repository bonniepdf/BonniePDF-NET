// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>Proportional unit grid (default 10x10) allocated by the deterministic largest-remainder method.</summary>
public class WaffleChart : ChartBase
{
    public int Rows { get; set; } = 10;
    public int Columns { get; set; } = 10;
    public string CellShape { get; set; } = "rounded";
    public float CellGap { get; set; } = 2;
    public string FillOrder { get; set; } = "rowsFromTop";
    public bool ShowLegend { get; set; } = true;
    public string ValueFormat { get; set; } = "P0";

    internal override bool UsesStyledGeometry => true;

    public WaffleChart() { AutoLayout = true; }
    public WaffleChart(ChartData data) : base(data) { AutoLayout = true; }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        IList<ChartDataPoint> points = data.Series.Count > 0 ? data.Series[0].DataPoints : [];
        if (points.Count == 0) return;
        if (Rows <= 0 || Columns <= 0) throw new InvalidOperationException("Waffle rows and columns must be positive integers.");
        if (points.Any(p => p.Value < 0)) throw new InvalidOperationException("Waffle values must be finite and nonnegative.");
        double total = points.Sum(p => (double)p.Value);
        if (!(total > 0)) return;

        int totalCells = Rows * Columns;
        double[] exact = points.Select(p => (double)p.Value / total * totalCells).ToArray();
        int[] counts = exact.Select(e => (int)Math.Floor(e)).ToArray();
        int remaining = totalCells - counts.Sum();
        int[] order = Enumerable.Range(0, points.Count)
            .OrderByDescending(i => exact[i] - counts[i])
            .ThenBy(i => i)
            .ToArray();
        for (int k = 0; k < remaining; k++) counts[order[k]]++;

        int[] cellCategory = new int[totalCells];
        int pos = 0;
        for (int cat = 0; cat < counts.Length; cat++)
            for (int k = 0; k < counts[cat]; k++) cellCategory[pos++] = cat;

        string font = ResolveFontFamily();
        double topEdge = (data.Title.Length > 0 ? 56 : 16) + (data.Subtitle.Length > 0 ? 18 : 0);
        double bottomEdge = Height - 16 - (Caption.Length > 0 ? 16 : 0);
        double legendW = ShowLegend ? Math.Min(Width * 0.38, 165) : 0;
        double availW = Math.Max(40, Width - 32 - legendW);
        double availH = Math.Max(40, bottomEdge - topEdge);
        double gap = Math.Max(0, (double)CellGap);
        double cellSize = Math.Max(4, Math.Min((availW - gap * (Columns - 1)) / Columns, (availH - gap * (Rows - 1)) / Rows));
        double gridW = cellSize * Columns + gap * (Columns - 1);
        double gridH = cellSize * Rows + gap * (Rows - 1);
        double startX = 16 + (availW - gridW) / 2;
        double startY = topEdge + (availH - gridH) / 2;
        Color[] colors = points.Select((p, i) => ChartCategoryColors.For(Appearance, p, i)).ToArray();

        bool rowsFromTop = string.Equals(FillOrder, "rowsFromTop", StringComparison.Ordinal);
        for (int idx = 0; idx < totalCells; idx++)
        {
            int r = rowsFromTop ? idx / Columns : idx % Rows;
            int c = rowsFromTop ? idx % Columns : idx / Rows;
            double x = startX + c * (cellSize + gap);
            double y = startY + r * (cellSize + gap);
            Color color = colors[cellCategory[idx]];
            if (string.Equals(CellShape, "circle", StringComparison.Ordinal))
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.CircleCommands(x + cellSize / 2, y + cellSize / 2, cellSize / 2), color, null));
            else if (string.Equals(CellShape, "rounded", StringComparison.Ordinal))
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RoundedRectCommands(x, y, cellSize, cellSize, cellSize * 0.22), color, null));
            else
                canvas.FillRect((float)x, (float)y, (float)cellSize, (float)cellSize, color);
        }

        if (ShowLegend)
        {
            double legX = startX + gridW + 20;
            const double rowH = 22;
            double legStartY = startY + Math.Max(0, (gridH - points.Count * rowH) / 2);
            for (int i = 0; i < points.Count; i++)
            {
                double y = legStartY + i * rowH;
                ChartVector.Fill(canvas, new ChartPathPlan(ChartArc.RoundedRectCommands(legX, y + 2, 10, 10, 2), colors[i], null));
                string share = ChartNumberFormat.Format(ChartTemplate.ToDecimal((double)points[i].Value / total), ValueFormat);
                canvas.DrawText((float)(legX + 16), (float)(y + 1), $"{points[i].Label} {share}", Appearance.LegendTextColor, 10, font, TextAlignment.Left);
            }
        }
    }
}
