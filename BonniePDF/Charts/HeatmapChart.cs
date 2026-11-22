// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// 2D categorical matrix heatmap with linear sRGB colour interpolation and WCAG auto-contrast cell values.
/// </summary>
public class HeatmapChart : ChartBase
{
    private const double RoundedK = 0.5522847498307933;
    private IList<Color> _colorScale = [Color.FromHex("#eff6ff"), Color.FromHex("#93c5fd"), Color.FromHex("#2563eb"), Color.FromHex("#1e3a8a")];

    /// <summary>Column category labels drawn along the top or bottom axis.</summary>
    public IList<string> XCategories { get; set; } = [];
    /// <summary>Row category labels drawn along the left axis.</summary>
    public IList<string> YCategories { get; set; } = [];
    /// <summary>2D matrix of numeric cell values (<c>null</c> represents an empty cell).</summary>
    public IList<IList<decimal?>> Values { get; set; } = [];
    /// <summary>Optional explicit lower bound for colour interpolation.</summary>
    public decimal? MinValue { get; set; }
    /// <summary>Optional explicit upper bound for colour interpolation.</summary>
    public decimal? MaxValue { get; set; }
    /// <summary>Gap between adjacent heatmap cells in points.</summary>
    public float CellGap { get; set; } = 2f;
    /// <summary>Corner radius of heatmap cells in points.</summary>
    public float CellCornerRadius { get; set; } = 2f;
    /// <summary>Whether to draw formatted values inside cells with automatic WCAG contrast.</summary>
    public bool ShowCellValues { get; set; } = true;
    /// <summary>Number format for cell and legend values.</summary>
    public string ValueFormat { get; set; } = "G";
    /// <summary>Prefix prepended to cell and legend values.</summary>
    public string ValuePrefix { get; set; } = "";
    /// <summary>Suffix appended to cell and legend values.</summary>
    public string ValueSuffix { get; set; } = "";
    /// <summary>Whether to draw the continuous colour scale legend.</summary>
    public bool ShowColorLegend { get; set; } = true;
    /// <summary>Fill colour used for cells whose value is <c>null</c>.</summary>
    public Color EmptyCellColor { get; set; } = Color.FromHex("#f8fafc");

    /// <summary>Ordered multi-stop colour ramp (at least 2 colours) interpolated across the value range.</summary>
    public IList<Color> ColorScale
    {
        get => _colorScale;
        set
        {
            if (value is null || value.Count < 2)
                throw new ArgumentOutOfRangeException(nameof(value), "HeatmapChart ColorScale requires at least 2 colours.");
            _colorScale = value;
        }
    }

    internal override bool UsesStyledGeometry => true;

    public HeatmapChart()
    {
        AutoLayout = true;
    }

    public HeatmapChart(ChartData data) : base(data)
    {
        AutoLayout = true;
    }

    private Color SampleScale(double t)
    {
        double clamped = Math.Clamp(t, 0.0, 1.0);
        int n = _colorScale.Count - 1;
        double scaled = clamped * n;
        int i = Math.Min(n - 1, (int)Math.Floor(scaled));
        double frac = scaled - i;
        Color a = _colorScale[i];
        Color b = _colorScale[i + 1];
        byte r = (byte)Math.Round(a.R + (b.R - a.R) * frac, MidpointRounding.AwayFromZero);
        byte g = (byte)Math.Round(a.G + (b.G - a.G) * frac, MidpointRounding.AwayFromZero);
        byte bl = (byte)Math.Round(a.B + (b.B - a.B) * frac, MidpointRounding.AwayFromZero);
        return new Color(r, g, bl, (byte)255);
    }

    private static List<ChartPathCommand> CellRectCommands(double x, double y, double w, double h, double radius)
    {
        double r = Math.Min(Math.Max(0, radius), Math.Min(w / 2.0, h / 2.0));
        if (r <= 0)
            return [new("M", [x, y]), new("L", [x + w, y]), new("L", [x + w, y + h]), new("L", [x, y + h]), new("Z", [])];
        double c = RoundedK * r;
        return
        [
            new("M", [x + r, y]),
            new("L", [x + w - r, y]),
            new("C", [x + w - r + c, y, x + w, y + r - c, x + w, y + r]),
            new("L", [x + w, y + h - r]),
            new("C", [x + w, y + h - r + c, x + w - r + c, y + h, x + w - r, y + h]),
            new("L", [x + r, y + h]),
            new("C", [x + r - c, y + h, x, y + h - r + c, x, y + h - r]),
            new("L", [x, y + r]),
            new("C", [x, y + r - c, x + r - c, y, x + r, y]),
            new("Z", []),
        ];
    }

    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        int rows = YCategories.Count;
        int cols = XCategories.Count;
        if (rows == 0 || cols == 0) return;

        decimal dMin = decimal.MaxValue, dMax = decimal.MinValue;
        bool found = false;
        for (int r = 0; r < rows; r++)
        {
            if (r >= Values.Count) continue;
            var row = Values[r];
            for (int c = 0; c < cols && c < row.Count; c++)
            {
                if (row[c] is decimal v)
                {
                    found = true;
                    if (v < dMin) dMin = v;
                    if (v > dMax) dMax = v;
                }
            }
        }
        decimal minV = MinValue ?? (found ? dMin : 0m);
        decimal maxV = MaxValue ?? (found ? (dMax > minV ? dMax : minV + 1m) : 1m);
        double span = maxV > minV ? (double)(maxV - minV) : 1.0;

        string font = ResolveFontFamily();
        float maxYLabel = 0;
        foreach (string lbl in YCategories)
        {
            float w = canvas.MeasureText(lbl, 10, font).Width;
            if (w > maxYLabel) maxYLabel = w;
        }

        float leftPad = MathF.Max(36, MathF.Min(Width * 0.3f, MathF.Ceiling(maxYLabel + 16)));
        const float rightPad = 20;
        float topBand = (!string.IsNullOrEmpty(data.Title) ? 56f : 18f) + (!string.IsNullOrEmpty(data.Subtitle) ? 18f : 0f);
        const float xLabelBand = 22f;
        float legendBand = ShowColorLegend ? 32f : 0f;
        float captionBand = !string.IsNullOrEmpty(Caption) ? 16f : 0f;

        float gridX = leftPad;
        float gridY = topBand + xLabelBand;
        float gridW = MathF.Max(40, Width - leftPad - rightPad);
        float gridH = MathF.Max(40, Height - gridY - 12 - legendBand - captionBand);

        float gap = MathF.Max(0, CellGap);
        float cellW = (gridW - gap * (cols - 1)) / cols;
        float cellH = (gridH - gap * (rows - 1)) / rows;

        for (int c = 0; c < cols; c++)
        {
            float cx = gridX + c * (cellW + gap) + cellW / 2f;
            canvas.DrawText(cx, topBand + 4, XCategories[c], Appearance.AxisColor, 10, font, TextAlignment.Center);
        }

        for (int r = 0; r < rows; r++)
        {
            float cy = gridY + r * (cellH + gap);
            canvas.DrawText(gridX - 8, cy + cellH / 2f - 5, YCategories[r], Appearance.AxisColor, 10, font, TextAlignment.Right);

            var row = r < Values.Count ? Values[r] : null;
            for (int c = 0; c < cols; c++)
            {
                float cx = gridX + c * (cellW + gap);
                decimal? raw = row is not null && c < row.Count ? row[c] : null;
                Color fill = raw.HasValue ? SampleScale(((double)raw.Value - (double)minV) / span) : EmptyCellColor;

                if (CellCornerRadius > 0)
                    ChartVector.Fill(canvas, new ChartPathPlan(CellRectCommands(cx, cy, cellW, cellH, CellCornerRadius), fill, null));
                else
                    canvas.FillRect(cx, cy, cellW, cellH, fill);

                if (raw.HasValue && ShowCellValues)
                {
                    string text = $"{ValuePrefix}{ChartNumberFormat.Format(raw.Value, ValueFormat)}{ValueSuffix}";
                    float fontSize = MathF.Min(10f, MathF.Max(7f, MathF.Floor(cellH * 0.45f)));
                    Size measured = canvas.MeasureText(text, fontSize, font);
                    if (measured.Width + 4 <= cellW && fontSize + 2 <= cellH)
                        canvas.DrawText(cx + cellW / 2f, cy + cellH / 2f - fontSize / 2f, text, ChartValueScale.ContrastTextColor(fill), fontSize, font, TextAlignment.Center);
                }
            }
        }

        if (ShowColorLegend)
        {
            float barW = MathF.Min(200f, gridW * 0.5f);
            const float barH = 10f;
            float barX = gridX + (gridW - barW) / 2f;
            float barY = gridY + gridH + 10f;
            List<ChartGradientStop> stops = new(_colorScale.Count);
            for (int idx = 0; idx < _colorScale.Count; idx++)
                stops.Add(new ChartGradientStop(idx / (double)(_colorScale.Count - 1), _colorScale[idx]));
            ChartVector.Fill(canvas, new ChartPathPlan(
                CellRectCommands(barX, barY, barW, barH, 3),
                _colorScale[0],
                new ChartResolvedGradient(barX, barY, barX + barW, barY, _colorScale[0], _colorScale[^1], stops)));
            string minText = $"{ValuePrefix}{ChartNumberFormat.Format(minV, ValueFormat)}{ValueSuffix}";
            string maxText = $"{ValuePrefix}{ChartNumberFormat.Format(maxV, ValueFormat)}{ValueSuffix}";
            canvas.DrawText(barX - 6, barY, minText, Appearance.AxisColor, 9, font, TextAlignment.Right);
            canvas.DrawText(barX + barW + 6, barY, maxText, Appearance.AxisColor, 9, font, TextAlignment.Left);
        }
    }
}
