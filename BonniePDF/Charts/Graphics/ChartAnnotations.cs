// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using BonniePDF.Drawing;
namespace BonniePDF;

/// <summary>
/// A place on a chart: a data point (<see cref="ForPoint"/>), a category and value (<see cref="ForCategory"/>), or a fraction of
/// the plot (<see cref="InPlot"/>) or of the chart (<see cref="InChart"/>).
/// </summary>
public sealed class ChartAnchor
{
    /// <summary>Series of a point anchor: an <see cref="int"/> index or a <see cref="string"/> name.</summary>
    public object? Series { get; set; }
    /// <summary>Point of a point anchor: an <see cref="int"/> index or a category label.</summary>
    public object? Point { get; set; }
    /// <summary>Category of a category anchor: an <see cref="int"/> index or a label.</summary>
    public object? Category { get; set; }
    /// <summary>Value of a category anchor, on the primary axis.</summary>
    public double? Value { get; set; }
    /// <summary>Horizontal fraction of a plot or chart anchor, from the left.</summary>
    public double? X { get; set; }
    /// <summary>Vertical fraction of a plot or chart anchor, from the top.</summary>
    public double? Y { get; set; }
    /// <summary>For a fraction anchor, <c>plot</c> or <c>chart</c>.</summary>
    public string? Space { get; set; }

    /// <summary>The value end of a mark. Each argument is an index or a name.</summary>
    public static ChartAnchor ForPoint(object series, object point) => new() { Series = series, Point = point };
    /// <summary>The centre of a category at a value on the primary axis.</summary>
    public static ChartAnchor ForCategory(object category, double value) => new() { Category = category, Value = value };
    /// <summary>Fractions of the plot rectangle, origin top-left.</summary>
    public static ChartAnchor InPlot(double x, double y) => new() { X = x, Y = y, Space = "plot" };
    /// <summary>Fractions of the chart, origin top-left.</summary>
    public static ChartAnchor InChart(double x, double y) => new() { X = x, Y = y, Space = "chart" };
}

/// <summary>The rounded box behind annotation text.</summary>
public sealed class ChartAnnotationBox
{
    /// <summary>Box fill. Unset uses the theme's text colour at 8 % over the background.</summary>
    public Color? Fill { get; set; }
    /// <summary>Border colour. Unset draws no border.</summary>
    public Color? BorderColor { get; set; }
    /// <summary>Border width in points (<c>1</c>).</summary>
    public float BorderWidth { get; set; } = 1;
    /// <summary>Corner radius in points (<c>4</c>).</summary>
    public float Radius { get; set; } = 4;
    /// <summary>Horizontal padding in points (<c>6</c>).</summary>
    public float PaddingX { get; set; } = 6;
    /// <summary>Vertical padding in points (<c>3</c>).</summary>
    public float PaddingY { get; set; } = 3;
}

/// <summary>
/// An annotation drawn over a Cartesian chart. Supported on bar, grouped bar, line and area charts; other charts throw when
/// they have annotations. Validated when the chart is drawn.
/// </summary>
public abstract class ChartAnnotation
{
    private protected ChartAnnotation() { }

    /// <summary>Accent colour of the annotation's lines or shapes.</summary>
    public Color? Color { get; set; }
    /// <summary>Text colour. Unset uses the theme's legend text colour.</summary>
    public Color? TextColor { get; set; }
    /// <summary>Text size in points (<c>10</c>).</summary>
    public float FontSize { get; set; } = 10;

    internal virtual void Validate()
    {
        if (!(FontSize > 0) || !float.IsFinite(FontSize)) throw new InvalidOperationException("Annotation fontSize must be positive.");
    }
}

/// <summary>Text at an anchor.</summary>
public sealed class ChartTextAnnotation : ChartAnnotation
{
    /// <summary>Where the text goes.</summary>
    public required ChartAnchor At { get; set; }
    /// <summary>The text.</summary>
    public required string Text { get; set; }
    /// <summary><c>start</c>, <c>center</c> (default) or <c>end</c>.</summary>
    public string Align { get; set; } = "center";
    /// <summary>An optional box behind the text.</summary>
    public ChartAnnotationBox? Box { get; set; }
}

/// <summary>A boxed label near a target, joined to it by a leader line.</summary>
public sealed class ChartCalloutAnnotation : ChartAnnotation
{
    /// <summary>The place the callout points at.</summary>
    public required ChartAnchor Target { get; set; }
    /// <summary>The label.</summary>
    public required string Text { get; set; }
    /// <summary><c>auto</c> (default), <c>above</c>, <c>below</c>, <c>left</c> or <c>right</c>.</summary>
    public string Placement { get; set; } = "auto";
    /// <summary><c>straight</c> (default), <c>elbow</c> or <c>none</c>.</summary>
    public string Leader { get; set; } = "straight";
    /// <summary>The label's box.</summary>
    public ChartAnnotationBox? Box { get; set; }
}

/// <summary>A line or arc between two anchors, with an optional arrowhead.</summary>
public sealed class ChartArrowAnnotation : ChartAnnotation
{
    /// <summary>Where the arrow starts.</summary>
    public required ChartAnchor From { get; set; }
    /// <summary>Where the arrow ends.</summary>
    public required ChartAnchor To { get; set; }
    /// <summary><c>triangle</c> (default), <c>open</c> or <c>none</c>.</summary>
    public string Head { get; set; } = "triangle";
    /// <summary><c>straight</c> (default) or <c>arc</c>.</summary>
    public string Curve { get; set; } = "straight";
    /// <summary>Line width in points (<c>1.5</c>).</summary>
    public float Width { get; set; } = 1.5f;

    internal override void Validate()
    {
        base.Validate();
        if (!(Width > 0)) throw new InvalidOperationException("Arrow width must be positive.");
    }
}

/// <summary>The difference arrow between two points, labelled with their growth.</summary>
public sealed class ChartGrowthAnnotation : ChartAnnotation
{
    /// <summary>The starting point (a point anchor).</summary>
    public required ChartAnchor From { get; set; }
    /// <summary>The ending point (a point anchor).</summary>
    public required ChartAnchor To { get; set; }
    /// <summary>Number format of the growth (<c>P0</c>).</summary>
    public string Format { get; set; } = "P0";
    /// <summary>Label template; <c>{growth}</c> is replaced by the formatted growth.</summary>
    public string Template { get; set; } = "{growth}";
}

/// <summary>The difference arrow between two points, labelled with their compound annual growth rate.</summary>
public sealed class ChartCagrAnnotation : ChartAnnotation
{
    /// <summary>The starting point (a point anchor).</summary>
    public required ChartAnchor From { get; set; }
    /// <summary>The ending point (a point anchor).</summary>
    public required ChartAnchor To { get; set; }
    /// <summary>Number of periods between the points; unset uses the category distance.</summary>
    public double? Periods { get; set; }
    /// <summary>Number format of the rate (<c>P1</c>).</summary>
    public string Format { get; set; } = "P1";
    /// <summary>Label template; <c>{cagr}</c> is replaced by the formatted rate.</summary>
    public string Template { get; set; } = "CAGR {cagr}";
}

/// <summary>A square bracket over a span of categories.</summary>
public sealed class ChartBracketAnnotation : ChartAnnotation
{
    /// <summary>First category: an index or a label.</summary>
    public required object FromCategory { get; set; }
    /// <summary>Last category: an index or a label.</summary>
    public required object ToCategory { get; set; }
    /// <summary><c>above</c> (default) or <c>below</c>.</summary>
    public string Side { get; set; } = "above";
    /// <summary>Optional text beside the bracket.</summary>
    public string? Text { get; set; }
}

/// <summary>A shaded span of categories across the plot height, drawn behind the marks.</summary>
public sealed class ChartRangeAnnotation : ChartAnnotation
{
    /// <summary>First category: an index or a label.</summary>
    public required object FromCategory { get; set; }
    /// <summary>Last category: an index or a label.</summary>
    public required object ToCategory { get; set; }
    /// <summary>The span's fill.</summary>
    public required ChartFill Fill { get; set; }
    /// <summary>Optional text inside the span.</summary>
    public string? Text { get; set; }
}

/// <summary>Dims every mark the selection does not include.</summary>
public sealed class ChartHighlightAnnotation : ChartAnnotation
{
    /// <summary>Series to keep: indexes or names.</summary>
    public IList<object>? Series { get; set; }
    /// <summary>Categories to keep: indexes or labels.</summary>
    public IList<object>? Categories { get; set; }
    /// <summary>Individual points to keep (point anchors).</summary>
    public IList<ChartAnchor>? Points { get; set; }
    /// <summary>The fraction of its colour a dimmed mark keeps, <c>0..1</c> (<c>0.25</c>).</summary>
    public double DimTo { get; set; } = 0.25;

    internal override void Validate()
    {
        base.Validate();
        if (Series is null && Categories is null && Points is null) throw new InvalidOperationException("A highlight annotation needs series, categories or points.");
        if (!(DimTo >= 0 && DimTo <= 1)) throw new InvalidOperationException("Highlight dimTo must be from 0 to 1.");
    }
}

/// <summary>A ring or shape around a point.</summary>
public sealed class ChartMarkerAnnotation : ChartAnnotation
{
    /// <summary>The place to mark.</summary>
    public required ChartAnchor Target { get; set; }
    /// <summary><c>ring</c> (default), <c>circle</c> or <c>square</c>.</summary>
    public string Shape { get; set; } = "ring";
    /// <summary>Size in points (<c>10</c>).</summary>
    public float Size { get; set; } = 10;

    internal override void Validate()
    {
        base.Validate();
        if (!(Size > 0)) throw new InvalidOperationException("Marker size must be positive.");
    }
}

/// <summary>An axis-aligned rectangle.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct AnnotationRect(double X, double Y, double Width, double Height);

/// <summary>What the painter needs to know about the chart it draws on.</summary>
internal sealed class AnnotationContext
{
    public required ICanvas Canvas { get; init; }
    public required string Family { get; init; }
    public required bool Vertical { get; init; }
    public required AnnotationRect Plot { get; init; }
    public required double ChartWidth { get; init; }
    public required double ChartHeight { get; init; }
    public required Color TextColor { get; init; }
    public required Color Background { get; init; }
    public required Color Positive { get; init; }
    public required Color Negative { get; init; }
    public required int SeriesCount { get; init; }
    public required double Slot { get; init; }
    public required double Origin { get; init; }
    public required double LabelAllowance { get; init; }
    public required Func<object, int> SeriesIndex { get; init; }
    public required Func<object, int> CategoryIndex { get; init; }
    public required Func<int, object, int> PointIndex { get; init; }
    public required Func<int, int, double> Value { get; init; }
    public required Func<int, int, Point> MarkPoint { get; init; }
    public required Func<int, double> CategoryCentre { get; init; }
    public required Func<double, double> ValueToCoordinate { get; init; }
    public required Func<int, int> CategoryPointCount { get; init; }
    public required Func<List<AnnotationRect>> MarkBoxes { get; init; }
}

/// <summary>The selection of marks a highlight keeps at full strength.</summary>
internal sealed class HighlightSelection
{
    private sealed record Parsed(int[]? Series, int[]? Categories, (int S, int P)[]? Points, double DimTo);
    private readonly List<Parsed> _parsed;

    private HighlightSelection(List<Parsed> parsed) { _parsed = parsed; Keep = parsed.Min(h => h.DimTo); }

    /// <summary>The fraction of its colour a dimmed mark keeps.</summary>
    public double Keep { get; }

    public bool Selects(int series, int point) => _parsed.Exists(h =>
        (h.Points?.Any(q => q.S == series && q.P == point) ?? false) ||
        ((h.Series is not null || h.Categories is not null) && (h.Series?.Contains(series) ?? true) && (h.Categories?.Contains(point) ?? true)));

    /// <summary>Whether it keeps the series as a whole, or <see langword="null"/> when no highlight chooses by series alone.</summary>
    public bool? SelectsSeries(int series)
    {
        var bySeries = _parsed.Where(h => h.Series is not null && h.Categories is null && h.Points is null).ToList();
        return bySeries.Count == 0 ? null : bySeries.Exists(h => h.Series!.Contains(series));
    }

    internal static HighlightSelection? Resolve(IEnumerable<ChartAnnotation> annotations, ChartAnnotationLookups lookups)
    {
        var list = annotations.OfType<ChartHighlightAnnotation>().ToList();
        if (list.Count == 0) return null;
        return new HighlightSelection(list.Select(h => new Parsed(
            h.Series?.Select(lookups.SeriesIndex).ToArray(),
            h.Categories?.Select(lookups.CategoryIndex).ToArray(),
            h.Points?.Select(p => { int s = lookups.SeriesIndex(p.Series!); return (s, lookups.PointIndex(s, p.Point!)); }).ToArray(),
            h.DimTo)).ToList());
    }
}

/// <summary>Resolves series, category and point references against a chart's data.</summary>
internal sealed record ChartAnnotationLookups(Func<object, int> SeriesIndex, Func<object, int> CategoryIndex, Func<int, object, int> PointIndex);

/// <summary>Draws annotations onto a sink.</summary>
internal sealed class ChartAnnotationPainter(AnnotationContext ctx)
{
    private const double CalloutGap = 22;
    private static readonly string[] Placements = ["above", "right", "below", "left"];
    private static bool Same(string? a, string b) => string.Equals(a, b, StringComparison.Ordinal);
    private double Out => ctx.Vertical ? -1 : 1;
    private Point At(double c, double h) => ctx.Vertical ? new Point((float)c, (float)h) : new Point((float)h, (float)c);
    private Color Accent(ChartAnnotation a) => a.Color ?? ctx.TextColor;
    private Color TextColour(ChartAnnotation a) => a.TextColor ?? ctx.TextColor;

    private static bool Intersects(AnnotationRect a, AnnotationRect b) => a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;

    private static List<ChartPathCommand> RectCommands(double x, double y, double w, double h, double radius)
    {
        double r = Math.Min(Math.Max(0, radius), Math.Min(w / 2, h / 2));
        if (r <= 0) return [new("M", [x, y]), new("L", [x + w, y]), new("L", [x + w, y + h]), new("L", [x, y + h]), new("Z", [])];
        double c = 4.0 / 3 * (Math.Sqrt(2) - 1) * r;
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

    internal Point Resolve(ChartAnchor anchor)
    {
        if (anchor.Series is not null)
        {
            int s = ctx.SeriesIndex(anchor.Series);
            return ctx.MarkPoint(s, ctx.PointIndex(s, anchor.Point ?? throw new InvalidOperationException("A point anchor needs a point.")));
        }
        if (anchor.Category is not null) return At(ctx.CategoryCentre(ctx.CategoryIndex(anchor.Category)), ctx.ValueToCoordinate(anchor.Value ?? throw new InvalidOperationException("A category anchor needs a value.")));
        double x = anchor.X ?? throw new InvalidOperationException("A fraction anchor needs x and y.");
        double y = anchor.Y ?? throw new InvalidOperationException("A fraction anchor needs x and y.");
        if (string.Equals(anchor.Space, "plot", StringComparison.Ordinal)) return new Point((float)(ctx.Plot.X + x * ctx.Plot.Width), (float)(ctx.Plot.Y + y * ctx.Plot.Height));
        if (string.Equals(anchor.Space, "chart", StringComparison.Ordinal)) return new Point((float)(x * ctx.ChartWidth), (float)(y * ctx.ChartHeight));
        throw new InvalidOperationException("An anchor needs a series and point, a category and value, or x, y and a space of plot or chart.");
    }

    internal void DrawBehind(IEnumerable<ChartAnnotation> annotations)
    {
        foreach (ChartRangeAnnotation a in annotations.OfType<ChartRangeAnnotation>())
        {
            int i = ctx.CategoryIndex(a.FromCategory);
            int j = ctx.CategoryIndex(a.ToCategory);
            double lo = ctx.CategoryCentre(Math.Min(i, j)) - ctx.Slot / 2;
            double hi = ctx.CategoryCentre(Math.Max(i, j)) + ctx.Slot / 2;
            ChartBox box = ctx.Vertical ? new(lo, ctx.Plot.Y, hi - lo, ctx.Plot.Height) : new(ctx.Plot.X, lo, ctx.Plot.Width, hi - lo);
            ChartFillBoxes boxes = new(box, new(ctx.Plot.X, ctx.Plot.Y, ctx.Plot.Width, ctx.Plot.Height), new(0, 0, ctx.ChartWidth, ctx.ChartHeight));
            ChartResolvedFill fill = a.Fill.Resolve(boxes);
            ChartVector.Fill(ctx.Canvas, new ChartPathPlan(RectCommands(box.X, box.Y, box.Width, box.Height, 0), fill.Color, fill.Gradient, fill.Radial));
            if (!string.IsNullOrEmpty(a.Text)) Text(a.Text, box.X + 6, box.Y + 4, "start", a.FontSize, TextColour(a));
        }
    }

    internal void DrawFront(IEnumerable<ChartAnnotation> annotations)
    {
        foreach (ChartAnnotation a in annotations)
        {
            switch (a)
            {
                case ChartTextAnnotation t: DrawText(t); break;
                case ChartCalloutAnnotation c: DrawCallout(c); break;
                case ChartArrowAnnotation r: DrawArrow(r); break;
                case ChartGrowthAnnotation g: DrawDifference(g, g.From, g.To, g.Format, g.Template, "{growth}", isGrowth: true, periods: null); break;
                case ChartCagrAnnotation g: DrawDifference(g, g.From, g.To, g.Format, g.Template, "{cagr}", isGrowth: false, g.Periods); break;
                case ChartBracketAnnotation b: DrawBracket(b); break;
                case ChartMarkerAnnotation m: DrawMarker(m); break;
            }
        }
    }

    private double Measure(string s, double size) => ctx.Canvas.MeasureText(s, (float)size, ctx.Family).Width;

    private void Text(string s, double x, double y, string align, double size, Color color)
    {
        ctx.Canvas.DrawText((float)x, (float)y, s, color, (float)size, ctx.Family, Same(align, "start") ? TextAlignment.Left : Same(align, "center") ? TextAlignment.Center : TextAlignment.Right);
    }

    private AnnotationRect Box(string s, double x, double y, double size, Color textColor, ChartAnnotationBox? box, Color? fillOverride = null)
    {
        double padX = box?.PaddingX ?? 6;
        double padY = box?.PaddingY ?? 3;
        double w = Measure(s, size) + 2 * padX;
        double h = size + 2 * padY;
        AnnotationRect rect = new(x, y, w, h);
        PaintBox(rect, box, fillOverride);
        ctx.Canvas.DrawText((float)(x + padX), (float)(y + padY), s, textColor, (float)size, ctx.Family, TextAlignment.Left);
        return rect;
    }

    private void PaintBox(AnnotationRect r, ChartAnnotationBox? box, Color? fillOverride = null)
    {
        Color fill = fillOverride ?? box?.Fill ?? ChartValueScale.BlendOverOpaque(ctx.TextColor.WithAlpha(0.08f), ctx.Background);
        double radius = box?.Radius ?? 4;
        ChartVector.Fill(ctx.Canvas, new ChartPathPlan(RectCommands(r.X, r.Y, r.Width, r.Height, radius), fill, null));
        if (box?.BorderColor is { } border)
        {
            float bw = box.BorderWidth;
            float half = bw / 2;
            float x0 = (float)r.X + half, x1 = (float)(r.X + r.Width) - half, y0 = (float)r.Y + half, y1 = (float)(r.Y + r.Height) - half;
            ctx.Canvas.DrawLine(x0, y0, x1, y0, border, bw);
            ctx.Canvas.DrawLine(x1, y0, x1, y1, border, bw);
            ctx.Canvas.DrawLine(x1, y1, x0, y1, border, bw);
            ctx.Canvas.DrawLine(x0, y1, x0, y0, border, bw);
        }
    }

    private (double X, double Y) Inside(double x, double y, double w, double h) =>
        (Math.Max(2, Math.Min(x, ctx.ChartWidth - w - 2)), Math.Max(2, Math.Min(y, ctx.ChartHeight - h - 2)));

    private void DrawText(ChartTextAnnotation a)
    {
        Point p = Resolve(a.At);
        double size = a.FontSize;
        string align = a.Align;
        Color color = TextColour(a);
        double w = Measure(a.Text, size);
        if (a.Box is null)
        {
            double left = Same(align, "start") ? p.X : Same(align, "center") ? p.X - w / 2 : p.X - w;
            var pos = Inside(left, p.Y - size / 2, w, size);
            Text(a.Text, Same(align, "start") ? pos.X : Same(align, "center") ? pos.X + w / 2 : pos.X + w, pos.Y, align, size, color);
            return;
        }
        double bw = w + 2 * a.Box.PaddingX;
        double bh = size + 2 * a.Box.PaddingY;
        double bl = Same(align, "start") ? p.X : Same(align, "center") ? p.X - bw / 2 : p.X - bw;
        var bp = Inside(bl, p.Y - bh / 2, bw, bh);
        Box(a.Text, bp.X, bp.Y, size, color, a.Box);
    }

    private void DrawCallout(ChartCalloutAnnotation a)
    {
        Point t = Resolve(a.Target);
        double size = a.FontSize;
        double w = Measure(a.Text, size) + 2 * (a.Box?.PaddingX ?? 6);
        double h = size + 2 * (a.Box?.PaddingY ?? 3);
        bool elbow = Same(a.Leader, "elbow");
        AnnotationRect Candidate(string placement) => placement switch
        {
            "above" => new(elbow ? t.X + 10 : t.X - w / 2, t.Y - CalloutGap - h, w, h),
            "below" => new(elbow ? t.X + 10 : t.X - w / 2, t.Y + CalloutGap, w, h),
            "right" => new(t.X + CalloutGap, elbow ? t.Y + 10 : t.Y - h / 2, w, h),
            _ => new(t.X - CalloutGap - w, elbow ? t.Y + 10 : t.Y - h / 2, w, h),
        };
        string placement;
        if (Same(a.Placement, "auto"))
        {
            List<AnnotationRect> marks = ctx.MarkBoxes();
            bool Fits(AnnotationRect r) => r.X >= 0 && r.Y >= 0 && r.X + r.Width <= ctx.ChartWidth && r.Y + r.Height <= ctx.ChartHeight && !marks.Exists(m => Intersects(r, m));
            placement = Placements.FirstOrDefault(p => Fits(Candidate(p))) ?? "above";
        }
        else placement = a.Placement;
        AnnotationRect raw = Candidate(placement);
        var pos = Inside(raw.X, raw.Y, w, h);
        Color accent = Accent(a);
        if (!Same(a.Leader, "none"))
        {
            bool vertical = placement is "above" or "below";
            if (elbow)
            {
                Point corner = vertical ? new Point(t.X, (float)(pos.Y + h / 2)) : new Point((float)(pos.X + w / 2), t.Y);
                Point end = vertical ? new Point((float)pos.X, (float)(pos.Y + h / 2)) : new Point((float)(pos.X + w / 2), (float)pos.Y);
                ctx.Canvas.DrawLine(t.X, t.Y, corner.X, corner.Y, accent, 1);
                ctx.Canvas.DrawLine(corner.X, corner.Y, end.X, end.Y, accent, 1);
            }
            else
            {
                Point edge = vertical
                    ? new Point((float)(pos.X + w / 2), (float)(Same(placement, "above") ? pos.Y + h : pos.Y))
                    : new Point((float)(Same(placement, "right") ? pos.X : pos.X + w), (float)(pos.Y + h / 2));
                ctx.Canvas.DrawLine(edge.X, edge.Y, t.X, t.Y, accent, 1);
            }
            ctx.Canvas.FillCircle(t.X, t.Y, 2, accent);
        }
        Box(a.Text, pos.X, pos.Y, size, TextColour(a), a.Box);
    }

    private void Head(Point tip, double dx, double dy, double length, string style, Color color, double width)
    {
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len == 0) len = 1;
        double ux = dx / len, uy = dy / len, half = length * 0.5;
        double bx = tip.X - ux * length, by = tip.Y - uy * length;
        Point left = new((float)(bx - uy * half), (float)(by + ux * half));
        Point right = new((float)(bx + uy * half), (float)(by - ux * half));
        if (Same(style, "triangle")) ctx.Canvas.FillPath([tip, left, right], color);
        else
        {
            ctx.Canvas.DrawLine(tip.X, tip.Y, left.X, left.Y, color, (float)width);
            ctx.Canvas.DrawLine(tip.X, tip.Y, right.X, right.Y, color, (float)width);
        }
    }

    private void Polyline(IReadOnlyList<Point> points, Color color, double width)
    {
        for (int i = 1; i < points.Count; i++) ctx.Canvas.DrawLine(points[i - 1].X, points[i - 1].Y, points[i].X, points[i].Y, color, (float)width);
    }

    private void DrawArrow(ChartArrowAnnotation a)
    {
        Point from = Resolve(a.From);
        Point to = Resolve(a.To);
        double width = a.Width;
        Color color = Accent(a);
        double headLength = 4 * width;
        double dx = to.X - from.X, dy = to.Y - from.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length == 0) return;
        List<Point> points = [];
        if (Same(a.Curve, "arc"))
        {
            double cx = (from.X + to.X) / 2 + dy / length * 0.4 * length;
            double cy = (from.Y + to.Y) / 2 - dx / length * 0.4 * length;
            for (int i = 0; i <= 16; i++)
            {
                double u = i / 16.0, v = 1 - u;
                points.Add(new Point((float)(v * v * from.X + 2 * v * u * cx + u * u * to.X), (float)(v * v * from.Y + 2 * v * u * cy + u * u * to.Y)));
            }
        }
        else points = [from, to];
        Point last = points[^1], before = points[^2];
        double ddx = last.X - before.X, ddy = last.Y - before.Y;
        if (Same(a.Head, "triangle"))
        {
            double dl = Math.Sqrt(ddx * ddx + ddy * ddy);
            if (dl == 0) dl = 1;
            points[^1] = new Point((float)(last.X - ddx / dl * headLength), (float)(last.Y - ddy / dl * headLength));
        }
        Polyline(points, color, width);
        if (!Same(a.Head, "none")) Head(last, ddx, ddy, headLength, a.Head, color, width);
    }

    private void DrawDifference(ChartAnnotation a, ChartAnchor fromAnchor, ChartAnchor toAnchor, string format, string template, string key, bool isGrowth, double? periods)
    {
        if (fromAnchor.Series is null || toAnchor.Series is null) throw new InvalidOperationException("A growth or cagr annotation needs point anchors.");
        int sa = ctx.SeriesIndex(fromAnchor.Series), pa = ctx.PointIndex(sa, fromAnchor.Point!);
        int sb = ctx.SeriesIndex(toAnchor.Series), pb = ctx.PointIndex(sb, toAnchor.Point!);
        double va = ctx.Value(sa, pa), vb = ctx.Value(sb, pb);
        Point from = ctx.MarkPoint(sa, pa), to = ctx.MarkPoint(sb, pb);
        double amount;
        if (isGrowth)
        {
            if (va == 0) throw new InvalidOperationException("A growth annotation needs a non-zero starting value.");
            amount = (vb - va) / Math.Abs(va);
        }
        else
        {
            if (!(va > 0) || !(vb > 0)) throw new InvalidOperationException("A cagr annotation needs positive values.");
            double n = periods ?? Math.Abs(pb - pa);
            if (!(n > 0)) throw new InvalidOperationException("A cagr annotation needs a positive number of periods.");
            amount = Math.Pow(vb / va, 1 / n) - 1;
        }
        decimal exact = decimal.Parse(amount.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);
        string label = template.Replace(key, ChartNumberFormat.Format(exact, format), StringComparison.Ordinal);
        double outward = Out;
        double ca = ctx.Vertical ? from.X : from.Y, cb = ctx.Vertical ? to.X : to.Y;
        double ha = ctx.Vertical ? from.Y : from.X, hb = ctx.Vertical ? to.Y : to.X;
        double size = a.FontSize;
        double pillH = size + 6;
        double pillW = Measure(label, size) + 12;
        double tallest = outward < 0 ? Math.Min(ha, hb) : Math.Max(ha, hb);
        double apex = tallest + outward * (12 + ctx.LabelAllowance);
        double dir = outward;
        double startA = ha + outward * ctx.LabelAllowance, startB = hb + outward * ctx.LabelAllowance;
        Point apexPoint = At((ca + cb) / 2, apex);
        bool leaves = ctx.Vertical ? apexPoint.Y - pillH / 2 < 2 : apexPoint.X + pillW / 2 > ctx.ChartWidth - 2;
        if (leaves)
        {
            dir = -outward;
            apex = ctx.Origin + dir * 14;
            startA = ctx.Origin;
            startB = ctx.Origin;
        }
        Color color = a.Color ?? (amount >= 0 ? ctx.Positive : ctx.Negative);
        const double headLength = 5;
        Point tip = At(cb, startB + dir * 3);
        Point shaftEnd = At(cb, startB + dir * (3 + headLength));
        Polyline([At(ca, startA + dir * 3), At(ca, apex), At(cb, apex), shaftEnd], color, 1.5);
        Head(tip, tip.X - shaftEnd.X, tip.Y - shaftEnd.Y, headLength, "triangle", color, 1.5);
        Point mid = At((ca + cb) / 2, apex);
        var rect = Inside(mid.X - pillW / 2, mid.Y - pillH / 2, pillW, pillH);
        PaintBox(new AnnotationRect(rect.X, rect.Y, pillW, pillH), new ChartAnnotationBox { Radius = (float)(pillH / 2) }, color);
        ctx.Canvas.DrawText((float)(rect.X + 6), (float)(rect.Y + 3), label, a.TextColor ?? ChartValueScale.ContrastTextColor(color), (float)size, ctx.Family, TextAlignment.Left);
    }

    private void DrawBracket(ChartBracketAnnotation a)
    {
        int i = ctx.CategoryIndex(a.FromCategory), j = ctx.CategoryIndex(a.ToCategory);
        int lo = Math.Min(i, j), hi = Math.Max(i, j);
        double dir = Same(a.Side, "below") ? -Out : Out;
        double extreme = ctx.Origin;
        for (int cat = lo; cat <= hi; cat++)
        {
            for (int s = 0; s < ctx.SeriesCount; s++)
            {
                if (cat >= ctx.CategoryPointCount(s)) continue;
                Point p = ctx.MarkPoint(s, cat);
                double h = ctx.Vertical ? p.Y : p.X;
                extreme = dir < 0 ? Math.Min(extreme, h) : Math.Max(extreme, h);
            }
        }
        double h0 = extreme + dir * 8;
        double c0 = ctx.CategoryCentre(lo) - ctx.Slot * 0.4;
        double c1 = ctx.CategoryCentre(hi) + ctx.Slot * 0.4;
        Polyline([At(c0, h0 - dir * 4), At(c0, h0), At(c1, h0), At(c1, h0 - dir * 4)], Accent(a), 1.25);
        if (string.IsNullOrEmpty(a.Text)) return;
        double size = a.FontSize;
        double w = Measure(a.Text, size);
        double mid = (c0 + c1) / 2;
        if (ctx.Vertical)
        {
            var pos = Inside(mid - w / 2, dir < 0 ? h0 - 3 - size : h0 + 3, w, size);
            Text(a.Text, pos.X + w / 2, pos.Y, "center", size, TextColour(a));
        }
        else
        {
            double x = dir > 0 ? h0 + 3 : h0 - 3;
            var pos = Inside(dir > 0 ? x : x - w, mid - size / 2, w, size);
            Text(a.Text, dir > 0 ? pos.X : pos.X + w, pos.Y, dir > 0 ? "start" : "end", size, TextColour(a));
        }
    }

    private void DrawMarker(ChartMarkerAnnotation a)
    {
        Point p = Resolve(a.Target);
        double size = a.Size;
        Color color = Accent(a);
        if (Same(a.Shape, "circle")) ctx.Canvas.FillCircle(p.X, p.Y, (float)(size / 2), color);
        else if (Same(a.Shape, "square"))
        {
            double h = size / 2;
            Polyline([new((float)(p.X - h), (float)(p.Y - h)), new((float)(p.X + h), (float)(p.Y - h)), new((float)(p.X + h), (float)(p.Y + h)), new((float)(p.X - h), (float)(p.Y + h)), new((float)(p.X - h), (float)(p.Y - h))], color, 1.5);
        }
        else
        {
            List<Point> points = [];
            for (int i = 0; i <= 24; i++)
            {
                double t = i / 24.0 * 2 * Math.PI;
                points.Add(new Point((float)(p.X + size / 2 * Math.Cos(t)), (float)(p.Y + size / 2 * Math.Sin(t))));
            }
            Polyline(points, color, 1.5);
        }
    }
}
