// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Base class for Cartesian charts (charts with X and Y axes).
/// </summary>
public abstract class CartesianChartBase : ChartBase
{
    private readonly List<ChartLabelBox> _labelBoxes = new();
    protected ChartData? _renderData;
    protected float _plotLeft = LeftPadding;
    protected float _plotRight = RightPadding;
    protected decimal _effectiveMin;
    protected decimal _effectiveMax = 10000;
    protected int _effectiveStepCount = 5;
    protected string _effectiveTickFormat = "N0";

    protected void DrawPlacedLabel(ICanvas canvas, float x, float y, string text, Color color, float size, string family, TextAlignment align)
    {
        if (Appearance.MeasuredLabels) ChartLabels.Draw(canvas, _labelBoxes, Width, Height, x, y, text, color, size, family, align);
        else canvas.DrawText(x, y, text, color, size, family, align);
    }

    protected Color ThemeAxisColor => Appearance.AxisColor;
    protected Color ThemeGridColor => Appearance.GridLineColor;

    protected CartesianChartBase() { }
    protected CartesianChartBase(ChartData data) : base(data) { }

    protected const float TitleAreaHeight = 60;
    protected const float BottomPadding = 70;
    protected const float TopPadding = 20;
    protected const float AxisLabelBand = 30;
    protected const float LeftPadding = 80;
    protected const float RightPadding = 40;
    protected const float AxisLineWidth = 1f;
    protected const float GridLineWidth = 0.5f;
    protected const float AxisLabelFontSize = 10;
    protected const float LegendFontSize = 11;

    protected static readonly Color AxisColor = ChartTheme.Legacy.AxisColor;
    protected static readonly Color GridLineColor = ChartTheme.Legacy.GridLineColor;

    public AxisOptions XAxis
    {
        get => Data.XAxis;
        set => Data.XAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    public AxisOptions YAxis
    {
        get => Data.YAxis;
        set => Data.YAxis = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string FontFamily { get; set; } = StandardFonts.Helvetica;
    public bool ShowValueLabels { get; set; } = true;
    public float LabelFontSize { get; set; } = 9;
    public string? ValueLabelFormat { get; set; }
    public string ValueLabelPrefix { get; set; } = "";
    public string ValueLabelSuffix { get; set; } = "";
    /// <summary>Where a negative value label's minus sign goes relative to its prefix: <c>minusAfterPrefix</c> (default), <c>minus</c> or <c>parentheses</c>.</summary>
    public string NegativeStyle { get; set; } = "minusAfterPrefix";

    /// <summary>Fill of the plot rectangle (a colour or a gradient), drawn behind references and gridlines.</summary>
    public ChartFill? PlotAreaFill { get; set; }
    /// <summary>Colour of a border drawn around the plot rectangle.</summary>
    public Color? PlotAreaBorderColor { get; set; }
    /// <summary>Width of the plot-area border in points.</summary>
    public float PlotAreaBorderWidth { get; set; } = 1;

    private ChartBox? _currentPlot;
    internal override ChartBox PlotRect => _currentPlot ?? ChartRect;

    /// <summary>Paints the plot rectangle's fill and border (draw layer L2).</summary>
    private void DrawPlotArea(ICanvas canvas, ChartBox plot)
    {
        ChartFill? plotFill = PlotAreaFill ?? Appearance.PlotAreaFill;
        Color? plotBorder = PlotAreaBorderColor ?? Appearance.PlotAreaBorderColor;
        if (Depth3d is { Walls: true })
        {
            (float dx, float dy) = DepthShift;
            double x = plot.X, y = plot.Y, w = plot.Width, h = plot.Height;
            ChartResolvedFill paint = plotFill is { } wallFill ? wallFill.Resolve(FillBoxes(new ChartBox(plot.X + dx, plot.Y - dy, plot.Width, plot.Height))) : new ChartResolvedFill(Color.FromHex("#f8fafc"));
            static List<ChartPathCommand> Poly(double[][] pts)
            {
                List<ChartPathCommand> commands = pts.Select((p, i) => new ChartPathCommand(i == 0 ? "M" : "L", [p[0], p[1]])).ToList();
                commands.Add(new ChartPathCommand("Z", []));
                return commands;
            }
            ChartVector.Fill(canvas, new ChartPathPlan(Poly([[x + dx, y - dy], [x + dx + w, y - dy], [x + dx + w, y + h - dy], [x + dx, y + h - dy]]), paint.Color, paint.Gradient, paint.Radial));
            ChartVector.Fill(canvas, new ChartPathPlan(Poly([[x, y + h], [x + w, y + h], [x + w + dx, y + h - dy], [x + dx, y + h - dy]]), paint.Color, paint.Gradient, paint.Radial));
        }
        else if (plotFill is { } fill)
        {
            double x = plot.X, y = plot.Y, w = plot.Width, h = plot.Height;
            List<ChartPathCommand> commands = [new("M", [x, y]), new("L", [x + w, y]), new("L", [x + w, y + h]), new("L", [x, y + h]), new("Z", [])];
            ChartResolvedFill resolved = fill.Resolve(FillBoxes(plot));
            ChartVector.Fill(canvas, new ChartPathPlan(commands, resolved.Color, resolved.Gradient, resolved.Radial));
        }
        if (plotBorder is { } border && PlotAreaBorderWidth > 0)
        {
            double inset = PlotAreaBorderWidth / 2.0, x = plot.X, y = plot.Y, w = plot.Width, h = plot.Height;
            ChartVector.Stroke(canvas, new ChartStrokePlan(
                [new("M", [x + inset, y + inset]), new("L", [x + w - inset, y + inset]), new("L", [x + w - inset, y + h - inset]), new("L", [x + inset, y + h - inset]), new("Z", [])],
                border, PlotAreaBorderWidth, "butt", "miter"));
        }
    }

    /// <summary>Effects for the bars of <paramref name="series"/>: the series style's, else the bar style's, else the theme's.</summary>
    internal static IList<ChartEffect>? ResolveBarEffects(ChartSeries series, IList<ChartEffect> barEffects)
    {
        if (series.Style?.Effects is { Count: > 0 } own) return own;
        return barEffects.Count > 0 ? barEffects : null;
    }

    /// <summary>Resolved effects for the bars of <paramref name="series"/>, including the theme's default.</summary>
    internal IReadOnlyList<ChartResolvedEffect>? ResolveBarEffectsResolved(ChartSeries series, IList<ChartEffect> barEffects) =>
        ChartEffect.ResolveAll(ResolveBarEffects(series, barEffects)) ?? (Appearance.BarEffects.Count > 0 ? Appearance.BarEffects : null);

    /// <summary>Resolved effects for the line of <paramref name="series"/>: the series style's, else the theme's.</summary>
    internal IReadOnlyList<ChartResolvedEffect>? ResolveLineEffects(ChartSeries series) =>
        ChartEffect.ResolveAll(series.Style?.Effects) ?? (Appearance.LineEffects.Count > 0 ? Appearance.LineEffects : null);

    /// <summary>The colour of the line and markers of <paramref name="series"/>: its own colour, else the theme's stroke palette, else its fill colour.</summary>
    internal Color ResolveSeriesStrokeColor(ChartSeries series)
    {
        IReadOnlyList<Color> palette = Appearance.StrokePalette;
        int index = (_renderData ?? Data).Series.IndexOf(series);
        Color baseColor = RawSeriesStrokeColor(series);
        return _highlight?.SelectsSeries(index) == false ? ChartTint.Dim(baseColor, EffectiveBackgroundColor ?? Color.White, _highlight.Keep) : baseColor;
    }

    /// <summary><see cref="ResolveSeriesStrokeColor"/> before any highlight dimming; value labels dim it per point instead.</summary>
    internal Color RawSeriesStrokeColor(ChartSeries series)
    {
        IReadOnlyList<Color> palette = Appearance.StrokePalette;
        if (!series.HasColor && palette.Count > 0) return palette[Math.Max(0, (_renderData ?? Data).Series.IndexOf(series)) % palette.Count];
        return ResolveSeriesColor(series);
    }

    internal (Color Color, ChartFill? Fill) ResolveMarkPaint(ChartSeries series, ChartDataPoint? dp, Color seriesColor)
    {
        ChartFill? f = dp?.Fill ?? (dp?.Color is { } pc ? ChartFill.Solid(pc) : series.Style?.Fill);
        (Color Color, ChartFill? Fill) paint = f is null ? (seriesColor, null) : f.IsSolid ? (f.FallbackColor, null) : (f.FallbackColor, f);
        Color dimmed = DimMark(series, dp is null ? -1 : series.DataPoints.IndexOf(dp), paint.Color);
        return dimmed.Equals(paint.Color) ? paint : (dimmed, null);
    }

    /// <summary><paramref name="color"/> as a mark drawn under the current highlight: unchanged when selected or when nothing is highlighted, else dimmed.</summary>
    internal Color DimMark(ChartSeries series, int pointIndex, Color color)
    {
        if (_highlight is null) return color;
        int s = (_renderData ?? Data).Series.IndexOf(series);
        return pointIndex >= 0 && _highlight.Selects(s, pointIndex) ? color : ChartTint.Dim(color, EffectiveBackgroundColor ?? Color.White, _highlight.Keep);
    }
    public string ValueLabelPosition { get; set; } = "outside";

    /// <summary>
    /// Annotations drawn over the chart: text, callouts, arrows, growth and CAGR brackets, category brackets, ranges, highlights and
    /// markers. Supported on bar, grouped bar, line and area charts; other charts throw when drawn with annotations.
    /// </summary>
    public IList<ChartAnnotation> Annotations { get; set; } = [];

    /// <summary>Regression lines over series of a category chart: linear, exponential, logarithmic, power, polynomial and moving average. Bar, grouped bar, line, area and dot plot charts.</summary>
    public IList<ChartTrendline> Trendlines { get; set; } = [];

    /// <summary>Thin lines from each point to the category axis. Used by line and area charts.</summary>
    internal void DrawDropLines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        float origin = ValueOrigin(data, chartAreaWidth, chartAreaHeight, baselineY);
        bool vertical = Orientation != ChartOrientation.Horizontal;
        foreach (ChartSeries series in data.Series)
        {
            Color color = RawSeriesStrokeColor(series);
            foreach (Point p in CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY))
            {
                if (vertical) canvas.DrawLine(p.X, p.Y, p.X, origin, color, 0.75f);
                else canvas.DrawLine(p.X, p.Y, origin, p.Y, color, 0.75f);
            }
        }
    }

    /// <summary>At each category a line from the lowest to the highest series value, and optionally a bar from the first to the last series.</summary>
    internal void DrawBandLines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, bool highLow, ChartUpDownBars? upDown)
    {
        if (!highLow && upDown is null) return;
        if (data.Series.Count < 2) throw new InvalidOperationException("highLowLines and upDownBars need at least two series.");
        float ratio = upDown?.WidthRatio ?? 0.3f;
        if (!(ratio > 0 && ratio <= 1)) throw new InvalidOperationException("upDownBars widthRatio must be above 0 and at most 1.");
        bool vertical = Orientation != ChartOrientation.Horizontal;
        List<IList<Point>> coords = data.Series.Select(s => CalculateSeriesCoordinates(s, data, chartAreaWidth, chartAreaHeight, baselineY)).ToList();
        int categories = coords.Max(c => c.Count);
        float slot = (vertical ? chartAreaWidth : chartAreaHeight) / Math.Max(1, categories);
        Color up = upDown?.UpColor ?? Appearance.PositiveColor ?? Color.FromHex("#15803d");
        Color down = upDown?.DownColor ?? Appearance.NegativeColor ?? Color.FromHex("#b91c1c");
        for (int i = 0; i < categories; i++)
        {
            List<Point> pts = coords.Where(c => i < c.Count).Select(c => c[i]).ToList();
            if (pts.Count < 2) continue;
            float H(Point p) => vertical ? p.Y : p.X;
            float c = vertical ? pts[0].X : pts[0].Y;
            Point lo = pts.Aggregate((a, b) => H(b) < H(a) ? b : a);
            Point hi = pts.Aggregate((a, b) => H(b) > H(a) ? b : a);
            if (upDown is not null)
            {
                float first = H(pts[0]), last = H(pts[^1]);
                bool rising = vertical ? last <= first : last >= first;
                Color color = rising ? up : down;
                float t = slot * ratio;
                float a = Math.Min(first, last);
                float len = Math.Max(1, Math.Abs(last - first));
                if (vertical) canvas.FillRect(c - t / 2, a, t, len, color);
                else canvas.FillRect(a, c - t / 2, len, t, color);
            }
            if (highLow) canvas.DrawLine(vertical ? c : H(lo), vertical ? H(lo) : c, vertical ? c : H(hi), vertical ? H(hi) : c, Appearance.LegendTextColor, 1);
        }
    }

    /// <summary>Position points by their <see cref="ChartDataPoint.Date"/> along a time axis instead of in equal category slots. Line, area and candlestick charts, vertical only.</summary>
    public ChartTimeAxis? TimeAxis { get; set; }

    private long _timeMin;
    private long _timeMax = 1;

    /// <summary>Whether this chart family can place points on a time axis.</summary>
    internal virtual bool SupportsTimeAxis => false;

    /// <summary>Whether the chart being drawn positions its points by date.</summary>
    internal bool TimeAxisActive => TimeAxis is not null;

    private void ResolveTimeScale(ChartData data)
    {
        if (TimeAxis is not { } axis) return;
        if (Orientation == ChartOrientation.Horizontal) throw new InvalidOperationException("A time axis needs a vertical chart.");
        List<long> times = data.Series.SelectMany(s => s.DataPoints.Select(p => p.Date is { } d ? ChartDates.Parse(d) : throw new InvalidOperationException("Every point needs a date on a time axis."))).ToList();
        long min = axis.MinDate is { } mn ? ChartDates.Parse(mn) : times.Min();
        long max = axis.MaxDate is { } mx ? ChartDates.Parse(mx) : times.Max();
        if (!(max > min)) { min -= 86_400_000; max += 86_400_000; }
        _timeMin = min;
        _timeMax = max;
    }

    /// <summary>The horizontal position of a point on the time axis, in chart coordinates.</summary>
    internal float TimeX(ChartDataPoint point, float chartAreaWidth) =>
        _plotLeft + (float)((double)(ChartDates.Parse(point.Date!.Value) - _timeMin) / (_timeMax - _timeMin) * chartAreaWidth);

    private string TimeUnit(float chartAreaWidth) => TimeAxis is { } a && !string.Equals(a.Unit, "auto", StringComparison.Ordinal) ? a.Unit : ChartDates.ChooseUnit(_timeMin, _timeMax, chartAreaWidth);

    private void DrawTimeGridLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        if (TimeAxis is not { ShowGridLines: true }) return;
        foreach (long t in ChartDates.Ticks(_timeMin, _timeMax, TimeUnit(chartAreaWidth)))
        {
            float x = _plotLeft + (float)((double)(t - _timeMin) / (_timeMax - _timeMin) * chartAreaWidth);
            canvas.DrawLine(x, topY, x, baselineY, ThemeGridColor, GridLineWidth);
        }
    }

    private void DrawTimeAxisLabels(ICanvas canvas, float chartAreaWidth, float baselineY)
    {
        string unit = TimeUnit(chartAreaWidth);
        string format = TimeAxis!.Format ?? ChartDates.DefaultFormat(unit);
        string family = EffectiveFontFamily;
        foreach (long t in ChartDates.Ticks(_timeMin, _timeMax, unit))
        {
            float x = _plotLeft + (float)((double)(t - _timeMin) / (_timeMax - _timeMin) * chartAreaWidth);
            canvas.DrawText(x, baselineY + 12, ChartDates.Format(t, format), ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
        }
    }

    /// <summary>Extra height families reserve below the category axis labels, such as a volume pane.</summary>
    internal virtual float GetExtraBottomBand(ChartData data) => 0;

    /// <summary>A grid of the plotted values beneath the plot: a row per series, a column per category. Vertical charts only.</summary>
    public ChartDataTable? DataTable { get; set; }

    private bool DataTableShown => DataTable is { Show: true };

    /// <summary>Height the data table takes below the category axis labels.</summary>
    private float DataTableHeight(ChartData data) => DataTableShown ? data.Series.Count * 16 + 10 : 0;

    private void DrawDataTable(ICanvas canvas, ChartData data, float chartAreaWidth, float baselineY)
    {
        if (DataTable is not { Show: true } table) return;
        string family = EffectiveFontFamily;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        float slot = chartAreaWidth / Math.Max(1, categories);
        float top = baselineY + 26;
        float size = LabelFontSize;
        Color gridColor = ThemeGridColor;
        canvas.DrawLine(_plotLeft, top - 3, _plotLeft + chartAreaWidth, top - 3, gridColor, GridLineWidth);
        for (int r = 0; r < data.Series.Count; r++)
        {
            ChartSeries series = data.Series[r];
            float y = top + r * 16;
            Color color = RawSeriesStrokeColor(series);
            float nameWidth = canvas.MeasureText(series.Name, size, family).Width;
            canvas.DrawText(_plotLeft - 8, y, series.Name, Appearance.LegendTextColor, size, family, TextAlignment.Right);
            if (table.ShowLegendKeys) canvas.FillRect(_plotLeft - 8 - nameWidth - 12, y + 1, 8, 8, color);
            for (int c = 0; c < series.DataPoints.Count; c++)
            {
                ChartDataPoint p = series.DataPoints[c];
                string text = table.Format is { } f ? ChartNumberFormat.Format(p.Value, f) : FormatPointValue(p);
                canvas.DrawText(_plotLeft + slot * (c + 0.5f), y, text, Appearance.LegendTextColor, size, family, TextAlignment.Center);
            }
            canvas.DrawLine(_plotLeft, y + 13, _plotLeft + chartAreaWidth, y + 13, gridColor, GridLineWidth);
        }
    }

    /// <summary>Oblique depth for bars: each bar gains a top face and a right face. Bar and grouped bar charts. Two-dimensional charts are easier to read accurately.</summary>
    public ChartDepth3d? Depth3d { get; set; }

    /// <summary>Whether this chart family can draw <see cref="Depth3d"/>.</summary>
    internal virtual bool SupportsDepth3d => false;

    /// <summary>How far full depth moves a point right and up; zero without <see cref="Depth3d"/>.</summary>
    internal (float Dx, float Dy) DepthShift => Depth3d?.Resolve() ?? (0f, 0f);

    /// <summary>Where the back wall sits relative to the front plane: the gridlines and wall move by this when <c>Depth3d.Walls</c> is on, else zero.</summary>
    private (float X, float Y) WallShift
    {
        get
        {
            if (Depth3d is not { Walls: true }) return (0f, 0f);
            (float dx, float dy) = DepthShift;
            return (dx, -dy);
        }
    }

    /// <summary>Draws the top and right faces of a bar whose front face is the rectangle, either sign of <paramref name="w"/> and <paramref name="h"/>.</summary>
    internal void DrawBarDepth(ICanvas canvas, float x, float y, float w, float h, Color color)
    {
        if (Depth3d is null) return;
        (float dx, float dy) = DepthShift;
        float left = w < 0 ? x + w : x;
        float top = h < 0 ? y + h : y;
        float width = MathF.Abs(w), height = MathF.Abs(h);
        if (width < 0.5f || height < 0.5f) return;
        void Face(float[][] pts, Color c)
        {
            List<ChartPathCommand> commands = pts.Select((p, i) => new ChartPathCommand(i == 0 ? "M" : "L", [p[0], p[1]])).ToList();
            commands.Add(new ChartPathCommand("Z", []));
            ChartVector.Fill(canvas, new ChartPathPlan(commands, c, null));
        }
        Face([[left, top], [left + width, top], [left + width + dx, top - dy], [left + dx, top - dy]], ChartTint.TintFace(color));
        Face([[left + width, top], [left + width + dx, top - dy], [left + width + dx, top + height - dy], [left + width, top + height]], ChartTint.ShadeFace(color));
    }

    private HighlightSelection? _highlight;

    /// <summary>Whether this chart family can place annotations: marks whose value end is a single point per category.</summary>
    internal virtual bool SupportsAnnotations => false;

    /// <summary>The thickness of a bar for annotation collision boxes, or <see langword="null"/> when marks are points.</summary>
    internal virtual float? AnnotationBarThickness(float slot, int seriesCount) => null;

    /// <summary>The value end of a mark, in chart coordinates. Families that offset marks within a slot override this.</summary>
    internal virtual Point AnnotationMarkPoint(ChartSeries series, int seriesIndex, int pointIndex, IList<Point> coordinates, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY) => coordinates[pointIndex];

    private static ChartAnnotationLookups AnnotationLookups(ChartData data)
    {
        List<string> categories = [];
        foreach (ChartSeries s in data.Series) if (s.DataPoints.Count > categories.Count) categories = s.DataPoints.Select(p => p.Label).ToList();
        static int Index(object reference, Func<string, int> byName, string kind)
        {
            int i = reference is int n ? n : reference is string name ? byName(name) : -1;
            return i >= 0 ? i : throw new InvalidOperationException($"Annotation anchor names a missing {kind} '{reference}'.");
        }
        return new ChartAnnotationLookups(
            reference =>
            {
                int i = Index(reference, name => data.Series.ToList().FindIndex(s => string.Equals(s.Name, name, StringComparison.Ordinal)), "series");
                return i < data.Series.Count ? i : throw new InvalidOperationException($"Annotation anchor names a missing series '{reference}'.");
            },
            reference =>
            {
                int i = Index(reference, name => categories.IndexOf(name), "category");
                return i < categories.Count ? i : throw new InvalidOperationException($"Annotation anchor names a missing category '{reference}'.");
            },
            (series, reference) =>
            {
                IList<ChartDataPoint> points = data.Series[series].DataPoints;
                int i = Index(reference, name => points.ToList().FindIndex(p => string.Equals(p.Label, name, StringComparison.Ordinal)), "point");
                return i < points.Count ? i : throw new InvalidOperationException($"Annotation anchor names a missing point '{reference}'.");
            });
    }

    private AnnotationContext AnnotationContext(ICanvas canvas, ChartData data, float width, float height, float baseline)
    {
        bool vertical = Orientation != ChartOrientation.Horizontal;
        ChartAnnotationLookups lookups = AnnotationLookups(data);
        int categoryCount = data.Series.Max(s => s.DataPoints.Count);
        float slot = (vertical ? width : height) / Math.Max(1, categoryCount);
        float origin = ValueOrigin(data, width, height, baseline);
        float topY = baseline - height;
        List<IList<Point>> coordinates = data.Series.Select(s => CalculateSeriesCoordinates(s, data, width, height, baseline)).ToList();
        Point MarkPoint(int s, int p) => AnnotationMarkPoint(data.Series[s], s, p, coordinates[s], data, width, height, baseline);
        return new AnnotationContext
        {
            Canvas = canvas,
            Family = EffectiveFontFamily,
            Vertical = vertical,
            Plot = new AnnotationRect(_plotLeft, topY, width, height),
            ChartWidth = Width,
            ChartHeight = Height,
            TextColor = Appearance.LegendTextColor,
            Background = EffectiveBackgroundColor ?? Color.White,
            Positive = Appearance.PositiveColor ?? Color.FromHex("#15803d"),
            Negative = Appearance.NegativeColor ?? Color.FromHex("#b91c1c"),
            SeriesCount = data.Series.Count,
            Slot = slot,
            Origin = origin,
            LabelAllowance = ShowValueLabels ? LabelFontSize + 7 : 0,
            SeriesIndex = lookups.SeriesIndex,
            CategoryIndex = lookups.CategoryIndex,
            PointIndex = lookups.PointIndex,
            Value = (s, p) => (double)data.Series[s].DataPoints[p].Value,
            MarkPoint = MarkPoint,
            CategoryCentre = i => (vertical ? _plotLeft : topY) + slot * (i + 0.5),
            ValueToCoordinate = v =>
            {
                double f = (double)Fraction((decimal)v);
                return vertical ? baseline - f * height : _plotLeft + f * width;
            },
            CategoryPointCount = s => data.Series[s].DataPoints.Count,
            MarkBoxes = () =>
            {
                float? thickness = AnnotationBarThickness(slot, data.Series.Count);
                List<AnnotationRect> boxes = [];
                for (int si = 0; si < data.Series.Count; si++)
                {
                    for (int p = 0; p < data.Series[si].DataPoints.Count; p++)
                    {
                        Point m = MarkPoint(si, p);
                        if (thickness is not { } t) boxes.Add(new AnnotationRect(m.X - 4, m.Y - 4, 8, 8));
                        else if (vertical) boxes.Add(new AnnotationRect(m.X - t / 2, Math.Min(m.Y, origin), t, Math.Abs(origin - m.Y)));
                        else boxes.Add(new AnnotationRect(Math.Min(m.X, origin), m.Y - t / 2, Math.Abs(m.X - origin), t));
                    }
                }
                return boxes;
            },
        };
    }
    public bool ShowLegend { get; set; } = true;
    public string LegendPosition { get; set; } = "bottom";
    public ChartOrientation Orientation { get; set; } = ChartOrientation.Vertical;
    public IList<ChartReference> References { get; set; } = [];
    public bool? ClipToPlotArea { get; set; }

    protected virtual bool DefaultAutoScale => false;
    protected virtual bool DefaultIncludeZero => true;
    protected virtual bool DefaultClipToPlotArea => false;
    protected virtual bool SupportsInlineLegend => false;
    protected bool EffectiveClipToPlotArea => ClipToPlotArea ?? DefaultClipToPlotArea;
    protected string EffectiveFontFamily => ResolveFontFamily(FontFamily);

    private protected virtual bool DrawCompact(ICanvas canvas, ChartData data) => false;

    protected virtual (double Min, double Max)? GetValueDataRange(ChartData data)
    {
        double min = double.PositiveInfinity;
        double max = double.NegativeInfinity;
        foreach (var series in data.Series)
        {
            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                double v = (double)series.DataPoints[i].Value;
                (double plus, double minus)? amounts = ErrorAmounts(series, i);
                double low = v - (amounts?.minus ?? 0);
                double high = v + (amounts?.plus ?? 0);
                if (low < min) min = low;
                if (high > max) max = high;
            }
        }
        return min <= max ? (min, max) : null;
    }

    /// <summary>The amounts below and above the value of a point that its error bars span, or <see langword="null"/> when it has none.</summary>
    internal static (double Plus, double Minus)? ErrorAmounts(ChartSeries series, int index)
    {
        ChartErrorBars? config = series.Style?.ErrorBars;
        ChartDataPoint point = series.DataPoints[index];
        if (config is null && point.ErrorPlus is null && point.ErrorMinus is null) return null;
        double plus, minus;
        if (config is null)
        {
            plus = (double?)point.ErrorPlus ?? (double?)point.ErrorMinus ?? 0;
            minus = (double?)point.ErrorMinus ?? (double?)point.ErrorPlus ?? 0;
        }
        else
        {
            config.Validate();
            List<double> values = series.DataPoints.Select(p => (double)p.Value).ToList();
            double baseAmount = config.Amount;
            if (string.Equals(config.Type, "percent", StringComparison.Ordinal)) baseAmount = Math.Abs((double)point.Value) * config.Amount / 100;
            else if (string.Equals(config.Type, "stdDev", StringComparison.Ordinal)) baseAmount = ChartTrendlineMath.SampleStandardDeviation(values) * config.Amount;
            else if (string.Equals(config.Type, "stdError", StringComparison.Ordinal)) baseAmount = ChartTrendlineMath.SampleStandardDeviation(values) / Math.Sqrt(values.Count) * config.Amount;
            plus = string.Equals(config.Direction, "minus", StringComparison.Ordinal) ? 0 : baseAmount;
            minus = string.Equals(config.Direction, "plus", StringComparison.Ordinal) ? 0 : baseAmount;
            if (point.ErrorPlus is { } p) plus = (double)p;
            if (point.ErrorMinus is { } m) minus = (double)m;
        }
        if (!(plus >= 0 && minus >= 0) || !double.IsFinite(plus + minus)) throw new InvalidOperationException("Error bar amounts must be finite and nonnegative.");
        return (plus, minus);
    }

    private void DrawErrorBars(ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        ChartErrorBars? config = series.Style?.ErrorBars;
        int seriesIndex = data.Series.IndexOf(series);
        bool vertical = Orientation != ChartOrientation.Horizontal;
        Color color = config?.Color ?? Appearance.LegendTextColor;
        float width = config?.Width ?? 1;
        float cap = config?.CapWidth ?? 6;
        for (int i = 0; i < series.DataPoints.Count; i++)
        {
            (double plus, double minus)? amounts = ErrorAmounts(series, i);
            if (amounts is not { } a) continue;
            if (!SupportsAnnotations) throw new InvalidOperationException($"Error bars are not supported on {GetType().Name}.");
            Point mark = AnnotationMarkPoint(series, seriesIndex, i, coordinates, data, chartAreaWidth, chartAreaHeight, baselineY);
            double value = (double)series.DataPoints[i].Value;
            double ToCoordinate(double v)
            {
                double f = (double)Fraction((decimal)v);
                return vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
            }
            double c = vertical ? mark.X : mark.Y;
            double low = ToCoordinate(value - a.minus), high = ToCoordinate(value + a.plus);
            Point At(double cc, double h) => vertical ? new Point((float)cc, (float)h) : new Point((float)h, (float)cc);
            Point p0 = At(c, low), p1 = At(c, high);
            canvas.DrawLine(p0.X, p0.Y, p1.X, p1.Y, color, width);
            foreach ((double amount, double h) in new[] { (a.minus, low), (a.plus, high) })
            {
                if (amount <= 0 || cap <= 0) continue;
                Point q0 = At(c - cap / 2, h), q1 = At(c + cap / 2, h);
                canvas.DrawLine(q0.X, q0.Y, q1.X, q1.Y, color, width);
            }
        }
    }

    internal virtual void DrawTrendlines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (Trendlines.Count == 0) return;
        if (TimeAxisActive) throw new InvalidOperationException("Trendlines cannot be combined with a time axis.");
        if (!SupportsAnnotations) throw new InvalidOperationException($"Trendlines are not supported on {GetType().Name}.");
        bool vertical = Orientation != ChartOrientation.Horizontal;
        int categories = data.Series.Max(s => s.DataPoints.Count);
        double axisStart = vertical ? _plotLeft : baselineY - chartAreaHeight;
        double slot = (vertical ? chartAreaWidth : chartAreaHeight) / (double)Math.Max(1, categories);
        string family = EffectiveFontFamily;
        foreach (ChartTrendline t in Trendlines)
        {
            t.Validate();
            int index = t.Series is int n ? n : t.Series is string name ? data.Series.ToList().FindIndex(s => string.Equals(s.Name, name, StringComparison.Ordinal)) : -1;
            if (index < 0 || index >= data.Series.Count) throw new InvalidOperationException($"Trendline names a missing series '{t.Series}'.");
            ChartSeries series = data.Series[index];
            List<double> xs = Enumerable.Range(1, series.DataPoints.Count).Select(i => (double)i).ToList();
            List<double> ys = series.DataPoints.Select(p => (double)p.Value).ToList();
            TrendlineFit fit = ChartTrendlineMath.Fit(t.Type, xs, ys, t.Order, t.Period);
            Color color = t.Color ?? RawSeriesStrokeColor(series);
            List<Point> pts = fit.Points.Select(p =>
            {
                double c = axisStart + slot * (p.X - 0.5);
                double f = (double)Fraction((decimal)p.Y);
                double h = vertical ? baselineY - f * chartAreaHeight : _plotLeft + f * chartAreaWidth;
                return vertical ? new Point((float)c, (float)h) : new Point((float)h, (float)c);
            }).ToList();
            StrokeFit(canvas, t, fit, pts, color, family, vertical);
        }
    }

    /// <summary>Draws a fitted trendline through its mapped points, with its equation and R² labels.</summary>
    internal static void StrokeFit(ICanvas canvas, ChartTrendline t, TrendlineFit fit, List<Point> pts, Color color, string family, bool vertical)
    {
        if (pts.Count < 2) return;
        List<ChartPathCommand> commands = pts.Select((p, i) => new ChartPathCommand(i == 0 ? "M" : "L", [p.X, p.Y])).ToList();
        IReadOnlyList<double> dash = t.DashPattern is { } d ? d.Select(v => (double)v).ToList() : [6, 4];
        ChartVector.Stroke(canvas, new ChartStrokePlan(commands, color, 1.5, "round", "round", 10, dash, 0, null));
        List<string> lines = [];
        if (t.Label is "equation" or "both" && fit.Equation is not null) lines.Add(fit.Equation);
        if (t.Label is "r2" or "both" && fit.R2 is { } r2) lines.Add("R² = " + ChartTrendlineMath.FormatSignificant(r2));
        Point end = pts[^1];
        for (int i = 0; i < lines.Count; i++)
        {
            float y = end.Y - 14 - (lines.Count - 1 - i) * 11;
            canvas.DrawText(vertical ? end.X : end.X - 4, y, lines[i], color, 9, family, TextAlignment.Right);
        }
    }

    /// <summary>Whether this chart family can draw error bars.</summary>
    internal virtual bool SupportsErrorBars => SupportsAnnotations;

    /// <summary>Whether the value axis being drawn is logarithmic.</summary>
    internal bool IsLogScale => string.Equals((_renderData ?? Data).YAxis.Scale, "log", StringComparison.Ordinal);

    /// <summary>Whether this chart family can draw a logarithmic value axis.</summary>
    internal virtual bool SupportsLogAxis => SupportsAnnotations;

    /// <summary>The fraction of the value axis, from the minimum (<c>0</c>) to the maximum (<c>1</c>), at which <paramref name="value"/> sits.</summary>
    internal decimal Fraction(decimal value) =>
        IsLogScale
            ? (value > 0 ? (decimal)ChartValueScale.LogFraction((double)value, (double)_effectiveMin, (double)_effectiveMax) : -1_000_000m) // a non-positive value is off the scale, and decimal cannot hold -Infinity
            : ChartValueScale.Fraction(value, _effectiveMin, _effectiveMax);

    /// <summary>Tick values of a logarithmic axis: the integer powers of its base within the range.</summary>
    internal List<double> LogTicks(double logBase)
    {
        int lo = (int)Math.Ceiling(Math.Log((double)_effectiveMin) / Math.Log(logBase) - 1e-9);
        int hi = (int)Math.Floor(Math.Log((double)_effectiveMax) / Math.Log(logBase) + 1e-9);
        List<double> ticks = [];
        for (int k = lo; k <= hi; k++) ticks.Add(Math.Pow(logBase, k));
        return ticks;
    }

    private void ResolveLogScale(ChartData data)
    {
        AxisOptions axis = data.YAxis;
        double logBase = axis.LogBase;
        if (!(logBase > 1) || !double.IsFinite(logBase)) throw new InvalidOperationException("logBase must be above 1.");
        if ((axis.HasMinValue && !(axis.MinValue > 0)) || (axis.HasMaxValue && !(axis.MaxValue > 0))) throw new InvalidOperationException("A logarithmic axis needs bounds above zero.");
        var range = GetValueDataRange(data);
        if (range is { } r0 && !(r0.Min > 0)) throw new InvalidOperationException("A logarithmic axis needs every value above zero.");
        double Exponent(double v) => Math.Log(v) / Math.Log(logBase);
        _effectiveMin = axis.HasMinValue ? axis.MinValue : range is not { } r1 ? 1m : (decimal)Math.Pow(logBase, Math.Floor(Exponent(r1.Min) + 1e-9));
        _effectiveMax = axis.HasMaxValue ? axis.MaxValue : range is not { } r2 ? (decimal)logBase : (decimal)Math.Pow(logBase, Math.Ceiling(Exponent(r2.Max) - 1e-9));
        if (!(_effectiveMax > _effectiveMin)) _effectiveMax = _effectiveMin * (decimal)logBase;
        _effectiveStepCount = Math.Max(1, (int)Math.Round(Exponent((double)(_effectiveMax / _effectiveMin)), MidpointRounding.AwayFromZero));
        _effectiveTickFormat = axis.ValueFormat ?? "G";
    }

    private void DrawLogGridLines(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY, bool dashed)
    {
        double logBase = data.YAxis.LogBase;
        bool horizontal = Orientation == ChartOrientation.Horizontal;
        (float wallX, float wallY) = WallShift;
        void Line(double v, float width)
        {
            float f = (float)Fraction((decimal)v);
            if (!(f > 0.0000001f && f <= 1.0000001f)) return;
            (float x1, float y1, float x2, float y2) = horizontal
                ? (_plotLeft + f * chartAreaWidth + wallX, topY + wallY, _plotLeft + f * chartAreaWidth + wallX, baselineY + wallY)
                : (_plotLeft + wallX, baselineY - f * chartAreaHeight + wallY, _plotLeft + chartAreaWidth + wallX, baselineY - f * chartAreaHeight + wallY);
            if (dashed) ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x1, y1]), new("L", [x2, y2])], ThemeGridColor, width, "butt", "miter", 10, [3, 3]));
            else canvas.DrawLine(x1, y1, x2, y2, ThemeGridColor, width);
        }
        foreach (double t in LogTicks(logBase)) Line(t, GridLineWidth);
        if (data.YAxis.ShowMinorGridLines)
        {
            int lo = (int)Math.Floor(Math.Log((double)_effectiveMin) / Math.Log(logBase) + 1e-9);
            int hi = (int)Math.Ceiling(Math.Log((double)_effectiveMax) / Math.Log(logBase) - 1e-9);
            for (int k = lo; k < hi; k++) for (int m = 2; m < logBase; m++) Line(m * Math.Pow(logBase, k), GridLineWidth / 2);
        }
    }

    protected virtual void ResolveValueScale(ChartData data)
    {
        AxisOptions axis = data.YAxis;
        if (string.Equals(axis.Scale, "log", StringComparison.Ordinal))
        {
            ResolveLogScale(data);
            return;
        }
        bool useAuto = axis.AutoScale || (DefaultAutoScale && !axis.HasMaxValue);
        if (useAuto)
        {
            var range = GetValueDataRange(data);
            if (range.HasValue)
            {
                var nice = ChartValueScale.ComputeNiceScale(
                    range.Value.Min,
                    range.Value.Max,
                    axis.StepCount,
                    axis.IncludeZero ?? DefaultIncludeZero,
                    axis.Headroom ?? (ShowValueLabels ? 0.05 : 0),
                    axis.HasMinValue ? (double)axis.MinValue : null,
                    axis.HasMaxValue ? (double)axis.MaxValue : null);
                _effectiveMin = nice.MinValue;
                _effectiveMax = nice.MaxValue;
                _effectiveStepCount = nice.StepCount;
                _effectiveTickFormat = axis.ValueFormat ?? nice.AutoFormat;
                return;
            }
        }
        _effectiveMin = axis.MinValue;
        _effectiveMax = axis.MaxValue;
        _effectiveStepCount = axis.StepCount > 0 ? axis.StepCount : 5;
        _effectiveTickFormat = axis.ValueFormat ?? "N0";
    }

    protected string FormatPointValue(ChartDataPoint dp, decimal? overrideValue = null)
    {
        decimal val = overrideValue ?? dp.Value;
        string fmt = dp.HasExplicitLabelFormat ? dp.LabelFormat : (ValueLabelFormat ?? dp.LabelFormat);
        return ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(val, fmt), ValueLabelPrefix, ValueLabelSuffix, NegativeStyle);
    }

    protected virtual float GetExtraRightMargin(ICanvas canvas, ChartData data) => 0f;

    protected virtual void DrawAfterSeries(ICanvas canvas, ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) { }

    protected bool ShouldDrawLegend(ChartData data) => !(DataTableShown && DataTable!.ShowLegendKeys) && ShowLegend && LegendPosition is not "none" && data.Series.Count > 1;

    protected string EffectiveLegendPosition() => LegendPosition is "inline" && !SupportsInlineLegend ? "top" : LegendPosition;

    protected sealed override void DrawContent(ICanvas canvas, ChartData data)
    {
        ChartData? previousData = _renderData;
        float prevLeft = _plotLeft;
        float prevRight = _plotRight;
        try
        {
            _labelBoxes.Clear();
            _renderData = data;
            if (data.Series.Count == 0) return;
            if (Annotations.Count > 0 && !SupportsAnnotations) throw new InvalidOperationException($"Annotations are not supported on {GetType().Name}.");
            if (DataTableShown && Orientation == ChartOrientation.Horizontal) throw new InvalidOperationException("A data table needs a vertical chart.");
            if (string.Equals(data.YAxis.Scale, "log", StringComparison.Ordinal) && !SupportsLogAxis) throw new InvalidOperationException($"A logarithmic axis is not supported on {GetType().Name}.");
            if (Depth3d is not null && !SupportsDepth3d) throw new InvalidOperationException($"3D depth is not supported on {GetType().Name}.");
            if (!SupportsErrorBars && data.Series.Any(s => s.Style?.ErrorBars is not null || s.DataPoints.Any(p => p.ErrorPlus is not null || p.ErrorMinus is not null)))
                throw new InvalidOperationException($"Error bars are not supported on {GetType().Name}.");
            _highlight = null;
            if (Annotations.Count > 0)
            {
                foreach (ChartAnnotation annotation in Annotations) annotation.Validate();
                _highlight = HighlightSelection.Resolve(Annotations, AnnotationLookups(data));
            }
            if (TimeAxis is not null && !SupportsTimeAxis) throw new InvalidOperationException($"A time axis is not supported on {GetType().Name}.");
            ResolveTimeScale(data);
            ResolveValueScale(data);
            ChartValueScale.Check(_effectiveMin, _effectiveMax);

            if (DrawCompact(canvas, data)) return;
            var layout = CalculateLayoutWithCanvas(canvas, data);
            _plotLeft = layout.PlotLeft;
            _plotRight = layout.PlotRight;

            _currentPlot = new ChartBox(_plotLeft, layout.TopY, layout.ChartAreaWidth, layout.ChartAreaHeight);
            DrawPlotArea(canvas, _currentPlot);

            DrawReferences(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, bands: true);
            ChartAnnotationPainter? painter = Annotations.Count > 0 ? new ChartAnnotationPainter(AnnotationContext(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY)) : null;
            painter?.DrawBehind(Annotations);

            if (data.YAxis.ShowGridLines && Appearance.GridLineStyle is not "none")
                DrawGridLines(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
            DrawTimeGridLines(canvas, layout.ChartAreaWidth, layout.BaselineY, layout.TopY);

            DrawYAxisLabels(canvas, data, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);

            if (!string.IsNullOrEmpty(data.YAxis.Legend))
                DrawYAxisLegend(canvas, data.YAxis, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.TopY, layout.BaselineY);

            if (EffectiveClipToPlotArea)
            {
                ChartVector.PushClipRect(canvas, _plotLeft, layout.TopY, layout.ChartAreaWidth, layout.ChartAreaHeight);
                try
                {
                    DrawAllSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
                }
                finally { ChartVector.PopClip(canvas); }
            }
            else
            {
                DrawAllSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
            }

            DrawAfterSeries(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);
            DrawTrendlines(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);
            DrawReferences(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, bands: false);

            if (ShowValueLabels)
                DrawValueLabels(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY, layout.TopY);

            DrawXAxisLabels(canvas, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);

            if (!string.IsNullOrEmpty(data.XAxis.Legend))
                DrawXAxisLegend(canvas, data.XAxis.Legend, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);

            DrawDataTable(canvas, data, layout.ChartAreaWidth, layout.BaselineY);

            if (Appearance.AxisFrame) DrawAxisFrameLines(canvas, layout.ChartAreaWidth, layout.BaselineY, layout.TopY);
            else
            {
                float baseline = Orientation == ChartOrientation.Horizontal ? layout.BaselineY : ValueOrigin(data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);
                canvas.DrawLine(_plotLeft, baseline, _plotLeft + layout.ChartAreaWidth, baseline, ThemeAxisColor, Appearance.AxisLineWidth);
            }

            painter?.DrawFront(Annotations);

            if (ShouldDrawLegend(data))
                DrawLegendInPosition(canvas, data, layout);
        }
        finally
        {
            _highlight = null;
            _currentPlot = null;
            _renderData = previousData;
            _plotLeft = prevLeft;
            _plotRight = prevRight;
            _labelBoxes.Clear();
        }
    }

    protected void DrawReferences(
        ICanvas canvas, ChartData data, float width, float height, float baseline, bool bands,
        IList<ChartReference>? customRefs = null, decimal? customMin = null, decimal? customMax = null, bool? forceHorizontal = null)
    {
        bool horizontal = forceHorizontal ?? (Orientation == ChartOrientation.Horizontal);
        decimal min = customMin ?? _effectiveMin;
        decimal max = customMax ?? _effectiveMax;
        IList<ChartReference> list = customRefs ?? References;
        foreach (ChartReference reference in list)
        {
            if (reference.EndValue.HasValue != bands) continue;
            float a = (float)(customMin is null ? Fraction(reference.Value) : ChartValueScale.Fraction(reference.Value, min, max));
            if (reference.EndValue is decimal end)
            {
                float b = (float)(customMin is null ? Fraction(end) : ChartValueScale.Fraction(end, min, max));
                float low = MathF.Max(0, MathF.Min(a, b));
                float high = MathF.Min(1, MathF.Max(a, b));
                if (!(high > low)) continue;
                float rx = horizontal ? _plotLeft + low * width : _plotLeft;
                float ry = horizontal ? baseline - height : baseline - high * height;
                float rw = horizontal ? (high - low) * width : width;
                float rh = horizontal ? height : (high - low) * height;
                canvas.FillRect(rx, ry, rw, rh, reference.Color);
                if (!string.IsNullOrEmpty(reference.Label))
                    DrawReferenceLabel(canvas, reference, rx, ry, rw, rh, true, horizontal);
            }
            else if (a >= 0 && a <= 1 && float.IsFinite(reference.Thickness) && reference.Thickness > 0)
            {
                float x = horizontal ? _plotLeft + a * width : _plotLeft;
                float y = baseline - (horizontal ? height : a * height);
                float len = horizontal ? height : width;
                DrawReferenceLine(canvas, x, y, len, horizontal, reference);
                if (!string.IsNullOrEmpty(reference.Label))
                    DrawReferenceLabel(canvas, reference, x, y, horizontal ? 0 : len, horizontal ? len : 0, false, horizontal);
            }
        }
    }

    private void DrawReferenceLabel(ICanvas canvas, ChartReference reference, float x, float y, float w, float h, bool isBand, bool verticalLine)
    {
        string label = reference.Label!;
        const float fontSize = 8.5f;
        string family = EffectiveFontFamily;
        Size measured = canvas.MeasureText(label, fontSize, family);
        string pos = string.IsNullOrEmpty(reference.LabelPosition) ? "end" : reference.LabelPosition;
        string placement = reference.LabelPlacement ?? (isBand ? "inside" : "above");
        Color textColor = reference.LabelColor ?? (reference.Color.ScA < 0.5f
            ? new Color(reference.Color.R, reference.Color.G, reference.Color.B, (byte)255)
            : reference.Color);
        if (!reference.LabelColor.HasValue && Appearance.AdaptiveReferenceLabels)
        {
            // A pale reference colour (a band, typically) is unreadable as text: fall back to the legend text colour.
            Color surface = reference.LabelBackgroundColor ?? EffectiveBackgroundColor ?? Color.White;
            Color opaque = surface.ScA < 1f ? ChartValueScale.BlendOverOpaque(surface, Color.White) : surface;
            Color fg = textColor.ScA < 1f ? ChartValueScale.BlendOverOpaque(textColor, opaque) : textColor;
            if (ChartValueScale.ContrastRatio(fg, opaque) < 4.5) textColor = Appearance.LegendTextColor;
        }

        float tx, ty;
        TextAlignment align;

        if (isBand)
        {
            if (pos is "start") { tx = x + 6; align = TextAlignment.Left; }
            else if (pos is "center") { tx = x + w / 2; align = TextAlignment.Center; }
            else { tx = x + w - 6; align = TextAlignment.Right; }
            ty = placement is "below" ? y + h + 3 : placement is "above" ? y - fontSize - 3 : y + 4;
        }
        else if (verticalLine)
        {
            tx = placement is "below" ? x - 4 : x + 4;
            align = placement is "below" ? TextAlignment.Right : TextAlignment.Left;
            ty = pos is "start" ? y + h - fontSize - 4 : pos is "center" ? y + h / 2 - fontSize / 2 : y + 4;
        }
        else
        {
            if (pos is "start") { tx = x + 4; align = TextAlignment.Left; }
            else if (pos is "center") { tx = x + w / 2; align = TextAlignment.Center; }
            else { tx = x + w - 4; align = TextAlignment.Right; }
            ty = placement is "below" ? y + 4 : y - fontSize - 4;
        }

        if (reference.LabelBackgroundColor.HasValue)
        {
            float px = align == TextAlignment.Right ? tx - measured.Width - 4 : align == TextAlignment.Center ? tx - measured.Width / 2 - 4 : tx - 4;
            float py = ty - 2;
            float pw = measured.Width + 8;
            float ph = fontSize + 4;
            const double r = 2;
            List<ChartPathCommand> cmds =
            [
                new("M", [px + r, py]),
                new("L", [px + pw - r, py]),
                new("L", [px + pw, py + r]),
                new("L", [px + pw, py + ph - r]),
                new("L", [px + pw - r, py + ph]),
                new("L", [px + r, py + ph]),
                new("L", [px, py + ph - r]),
                new("L", [px, py + r]),
                new("Z", []),
            ];
            ChartVector.Fill(canvas, new ChartPathPlan(cmds, reference.LabelBackgroundColor.Value, null));
        }

        canvas.DrawText(tx, ty, label, textColor, fontSize, family, align);
    }

    private static void DrawReferenceLine(ICanvas canvas, float x, float y, float length, bool vertical, ChartReference reference)
    {
        void Draw(float from, float to)
        {
            if (vertical) canvas.DrawLine(x, y + from, x, y + to, reference.Color, reference.Thickness);
            else canvas.DrawLine(x + from, y, x + to, y, reference.Color, reference.Thickness);
        }

        if (reference.DashPattern is not { Length: > 0 } pattern)
        {
            Draw(0, length);
            return;
        }

        foreach (float dash in pattern)
        {
            if (!float.IsFinite(dash) || dash <= 0 || length / dash > 100_000)
                throw new ArgumentOutOfRangeException(nameof(reference), "Reference dash lengths must be finite and positive and allow at most 100,000 segments.");
        }
        float from = 0;
        for (int i = 0; from < length; i++)
        {
            float to = MathF.Min(length, from + pattern[i % pattern.Length]);
            if (i % 2 == 0) Draw(from, to);
            from = to;
        }
    }

    protected virtual void DrawAllSeries(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        for (int seriesIndex = 0; seriesIndex < data.Series.Count; seriesIndex++)
        {
            ChartSeries series = data.Series[seriesIndex];
            int index = seriesIndex;
            IList<Point> coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            ChartVector.WithGroup(canvas, () => new ChartGroupInfo("series", SeriesIndex: index, Name: series.Name), () =>
            {
                DrawSeriesElements(canvas, data, series, coordinates, chartAreaWidth, chartAreaHeight, baselineY, topY);
                DrawErrorBars(canvas, data, series, coordinates, chartAreaWidth, chartAreaHeight, baselineY);
            });
        }
    }

    /// <summary>Draws one mark of a category chart (a bar, a segment, a point) inside a mark group.</summary>
    internal void MarkGroup(ICanvas canvas, ChartData data, ChartSeries series, int pointIndex, ChartGroupAnchor? anchor, Action draw, decimal? value = null) =>
        ChartVector.WithGroup(canvas, () =>
        {
            ChartDataPoint point = series.DataPoints[pointIndex];
            string description = DescribeMark("{series}, {label}: {value}", new Dictionary<string, object>
            {
                ["series"] = series.Name,
                ["label"] = point.Label,
                ["value"] = FormatPointValue(point, value),
                ["raw"] = value ?? point.Value,
            });
            return new ChartGroupInfo("mark", SeriesIndex: data.Series.IndexOf(series), PointIndex: pointIndex, Name: point.Label, Description: description, Anchor: anchor);
        }, draw);

    protected IList<Point> CalculateSeriesCoordinates(
        ChartSeries series, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        int dataPointCount = series.DataPoints.Count;
        List<Point> coordinates = new();
        if (dataPointCount == 0) return coordinates;

        if (TimeAxisActive)
        {
            for (int i = 0; i < dataPointCount; i++)
                coordinates.Add(new Point(TimeX(series.DataPoints[i], chartAreaWidth), baselineY - (float)(Fraction(series.DataPoints[i].Value) * (decimal)chartAreaHeight)));
            return coordinates;
        }

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = Fraction(series.DataPoints[i].Value);
                float x = _plotLeft + (float)(normalizedValue * (decimal)chartAreaWidth);
                coordinates.Add(new Point(x, y));
            }
        }
        else
        {
            float totalSpace = chartAreaWidth / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float x = _plotLeft + i * totalSpace + totalSpace / 2;
                decimal normalizedValue = Fraction(series.DataPoints[i].Value);
                float y = baselineY - (float)(normalizedValue * (decimal)chartAreaHeight);
                coordinates.Add(new Point(x, y));
            }
        }

        return coordinates;
    }

    internal float ValueOrigin(ChartData data, float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        decimal fraction = ChartValueScale.OriginFraction(_effectiveMin, _effectiveMax);
        return Orientation == ChartOrientation.Horizontal
            ? _plotLeft + (float)(fraction * (decimal)chartAreaWidth)
            : baselineY - (float)(fraction * (decimal)chartAreaHeight);
    }

    internal static void DrawConnectingLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth)
    {
        if (coordinates.Count < 2) return;
        for (int i = 0; i < coordinates.Count - 1; i++)
        {
            var p1 = coordinates[i];
            var p2 = coordinates[i + 1];
            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y, color, lineWidth);
        }
    }

    internal void DrawSeriesLine(ICanvas canvas, IList<Point> coordinates, Color color, float lineWidth, string curve = "linear", float[]? dashPattern = null, IReadOnlyList<ChartResolvedEffect>? effects = null)
    {
        if (coordinates.Count < 2) return;
        bool hasDash = dashPattern is { Length: > 0 };
        if (curve is "linear" && !hasDash && effects is not { Count: > 0 })
        {
            DrawConnectingLine(canvas, coordinates, color, lineWidth);
            return;
        }
        double[]? dashDouble = null;
        if (hasDash)
        {
            dashDouble = new double[dashPattern!.Length];
            for (int i = 0; i < dashPattern.Length; i++) dashDouble[i] = dashPattern[i];
            ChartCurves.ValidateDashPattern(dashDouble, ChartCurves.PolylineLength((IReadOnlyList<Point>)coordinates));
        }
        var cmds = ChartCurves.BuildCurveCommands((IReadOnlyList<Point>)coordinates, curve, Orientation == ChartOrientation.Horizontal);
        ChartVector.Stroke(canvas, new ChartStrokePlan(cmds, color, lineWidth, hasDash ? "butt" : "round", "round", 10, dashDouble, 0, effects));
    }

    protected abstract void DrawSeriesElements(
        ICanvas canvas, ChartData data, ChartSeries series, IList<Point> coordinates,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY);

    protected void DrawBarValueLabel(
        ICanvas canvas, float barX, float barY, float barW, float barH, string text, Color barColor,
        bool below, float offset, float pointX, float pointY, bool outsideAlignCenter = true, Color? outsideColor = null)
    {
        Color labelColor = outsideColor ?? barColor;
        string family = EffectiveFontFamily;
        if (ValueLabelPosition is "inside" or "auto")
        {
            Size measured = canvas.MeasureText(text, LabelFontSize, family);
            float absW = MathF.Abs(barW);
            float absH = MathF.Abs(barH);
            if (absW >= measured.Width + 6 && absH >= LabelFontSize + 6)
            {
                float rx = barW < 0 ? barX + barW : barX;
                float ry = barH < 0 ? barY + barH : barY;
                Color contrast = ChartValueScale.ContrastTextColor(barColor);
                canvas.DrawText(rx + absW / 2, ry + absH / 2 - LabelFontSize / 2, text, contrast, LabelFontSize, family, TextAlignment.Center);
                return;
            }
            if (ValueLabelPosition is "inside") return;
        }

        if (Orientation == ChartOrientation.Horizontal)
        {
            DrawPlacedLabel(canvas, below ? pointX - offset : pointX + offset, pointY - LabelFontSize / 2, text, labelColor, LabelFontSize, family, below ? TextAlignment.Right : TextAlignment.Left);
        }
        else
        {
            DrawPlacedLabel(canvas, pointX, below ? pointY + offset : pointY - offset - LabelFontSize, text, labelColor, LabelFontSize, family, outsideAlignCenter ? TextAlignment.Center : TextAlignment.Left);
        }
    }

    protected virtual void DrawValueLabels(
        ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        if (!ShowValueLabels) return;
        string family = EffectiveFontFamily;

        foreach (ChartSeries series in data.Series)
        {
            IList<Point> coordinates = CalculateSeriesCoordinates(series, data, chartAreaWidth, chartAreaHeight, baselineY);
            Color rawLabelColor = RawSeriesStrokeColor(series);
            float offset = GetLabelVerticalOffset(series);

            for (int i = 0; i < series.DataPoints.Count; i++)
            {
                Color labelColor = DimMark(series, i, rawLabelColor);
                Point point = coordinates[i];
                string text = FormatPointValue(series.DataPoints[i]);
                bool below = ChartValueScale.IsBelowOrigin(series.DataPoints[i].Value, _effectiveMin, _effectiveMax);

                if (Orientation == ChartOrientation.Horizontal)
                {
                    DrawPlacedLabel(canvas, below ? point.X - offset : point.X + offset, point.Y - LabelFontSize / 2, text, labelColor, LabelFontSize, family, below ? TextAlignment.Right : TextAlignment.Left);
                }
                else
                {
                    DrawPlacedLabel(canvas, point.X, below ? point.Y + offset : point.Y - offset - LabelFontSize, text, labelColor, LabelFontSize, family, TextAlignment.Center);
                }
            }
        }
    }

    protected virtual float GetLabelVerticalOffset(ChartSeries series) => 7;

    protected virtual void DrawLegendSwatch(ICanvas canvas, float x, float y, float size, ChartSeries series) =>
        canvas.FillRect(x, y, size, size, ResolveSeriesColor(series));

    private void DrawLegendInPosition(ICanvas canvas, ChartData data, (float PlotLeft, float PlotRight, float ChartAreaWidth, float ChartAreaHeight, float BaselineY, float TopY) layout)
    {
        string pos = EffectiveLegendPosition();
        if (pos is "none") return;
        if (pos is "bottom") { DrawLegend(canvas, data, layout.ChartAreaWidth); return; }
        string family = EffectiveFontFamily;

        if (pos is "top")
        {
            ChartTheme t = Appearance;
            float y = !string.IsNullOrEmpty(data.Title) ? t.TitleY + t.TitleFontSize + (!string.IsNullOrEmpty(data.Subtitle) ? 19 : 8) : 10;
            float x = _plotLeft;
            foreach (ChartSeries series in data.Series)
            {
                float w = canvas.MeasureText(series.Name, 11, family).Width + 32;
                if (x + w > Width - 16 && x > _plotLeft) { x = _plotLeft; y += 18; }
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
                x += w;
            }
            return;
        }
        if (pos is "right")
        {
            const float rowH = 20;
            float totalH = data.Series.Count * rowH;
            float startY = layout.TopY + MathF.Max(0, (layout.ChartAreaHeight - totalH) / 2);
            float x = _plotLeft + layout.ChartAreaWidth + 16;
            for (int i = 0; i < data.Series.Count; i++)
            {
                ChartSeries series = data.Series[i];
                float y = startY + i * rowH;
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
            }
            return;
        }
        if (pos is "inline")
        {
            List<(ChartSeries Series, float TargetY, float Y)> items = new();
            foreach (ChartSeries series in data.Series)
            {
                var coords = CalculateSeriesCoordinates(series, data, layout.ChartAreaWidth, layout.ChartAreaHeight, layout.BaselineY);
                if (coords.Count == 0) continue;
                Point last = coords[^1];
                items.Add((series, last.Y - 5.5f, last.Y - 5.5f));
            }
            items.Sort((a, b) => a.TargetY.CompareTo(b.TargetY));
            const float minGap = 14;
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].Y < items[i - 1].Y + minGap) items[i] = (items[i].Series, items[i].TargetY, items[i - 1].Y + minGap);
            }
            float maxY = layout.BaselineY - 11;
            if (items.Count > 0 && items[^1].Y > maxY)
            {
                items[^1] = (items[^1].Series, items[^1].TargetY, maxY);
                for (int i = items.Count - 2; i >= 0; i--)
                {
                    if (items[i].Y > items[i + 1].Y - minGap) items[i] = (items[i].Series, items[i].TargetY, items[i + 1].Y - minGap);
                }
            }
            float x = _plotLeft + layout.ChartAreaWidth + 8;
            foreach (var item in items)
                canvas.DrawText(x, item.Y, item.Series.Name, ResolveSeriesColor(item.Series), 10, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawLegend(ICanvas canvas, ChartData data, float chartAreaWidth)
    {
        string family = EffectiveFontFamily;
        float captionLift = !string.IsNullOrEmpty(Caption) ? 16 : 0;
        if (Appearance.FlowLegend)
        {
            float x = _plotLeft, y = Height - 42 - captionLift;
            foreach (ChartSeries series in data.Series)
            {
                float w = canvas.MeasureText(series.Name, 11, family).Width + 32;
                if (x + w > Width - 16 && x > _plotLeft) { x = _plotLeft; y += 18; }
                if (y + 12 > Height - 4 - captionLift) break;
                DrawLegendSwatch(canvas, x, y, 12, series);
                canvas.DrawText(x + 18, y, series.Name, ThemeAxisColor, 11, family, TextAlignment.Left);
                x += w;
            }
            return;
        }
        float legendY = Height - 40 - captionLift;
        float legendStartX = _plotLeft;
        const float legendItemWidth = 150;
        const float colorBoxSize = 12;

        for (int i = 0; i < data.Series.Count; i++)
        {
            ChartSeries series = data.Series[i];
            float x = legendStartX + i * legendItemWidth;
            DrawLegendSwatch(canvas, x, legendY, colorBoxSize, series);
            canvas.DrawText(x + colorBoxSize + 5, legendY + colorBoxSize / 2 - LegendFontSize / 2, series.Name, ThemeAxisColor, LegendFontSize, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawGridLines(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        bool dashed = Appearance.GridLineStyle is "dashed";

        if (IsLogScale)
        {
            DrawLogGridLines(canvas, data, chartAreaWidth, chartAreaHeight, baselineY, topY, dashed);
            return;
        }
        (float wallX, float wallY) = WallShift;
        if (Orientation == ChartOrientation.Horizontal)
        {
            float stepWidth = chartAreaWidth / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float x = _plotLeft + i * stepWidth + wallX;
                if (dashed)
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x, topY + wallY]), new("L", [x, baselineY + wallY])], ThemeGridColor, GridLineWidth, "butt", "miter", 10, [3, 3]));
                else
                    canvas.DrawLine(x, topY + wallY, x, baselineY + wallY, ThemeGridColor, GridLineWidth);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;
            for (int i = 1; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight + wallY;
                float x1 = _plotLeft + wallX;
                float x2 = _plotLeft + chartAreaWidth + wallX;
                if (dashed)
                    ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x1, y]), new("L", [x2, y])], ThemeGridColor, GridLineWidth, "butt", "miter", 10, [3, 3]));
                else
                    canvas.DrawLine(x1, y, x2, y, ThemeGridColor, GridLineWidth);
            }
        }
    }

    protected void DrawAxisFrameLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        canvas.BeginCrispEdges();
        canvas.DrawLine(_plotLeft, topY, _plotLeft, baselineY, ThemeAxisColor, AxisLineWidth);
        canvas.DrawLine(_plotLeft, baselineY, _plotLeft + chartAreaWidth, baselineY, ThemeAxisColor, AxisLineWidth);
        canvas.EndCrispEdges();
    }

    protected static void DrawAxisLines(ICanvas canvas, float chartAreaWidth, float baselineY, float topY)
    {
        canvas.BeginCrispEdges();
        canvas.DrawLine(LeftPadding, topY, LeftPadding, baselineY, AxisColor, AxisLineWidth);
        canvas.DrawLine(LeftPadding, baselineY, LeftPadding + chartAreaWidth, baselineY, AxisColor, AxisLineWidth);
        canvas.EndCrispEdges();
    }

    protected virtual void DrawYAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaHeight, float baselineY, float topY)
    {
        int stepCount = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
        decimal minValue = _effectiveMin;
        decimal maxValue = _effectiveMax;
        decimal stepValue = (maxValue - minValue) / stepCount;
        string family = EffectiveFontFamily;
        string fmt = _effectiveTickFormat;

        if (IsLogScale)
        {
            bool horizontalLog = Orientation == ChartOrientation.Horizontal;
            float logWidth = Width - _plotLeft - _plotRight;
            foreach (double v in LogTicks(data.YAxis.LogBase))
            {
                float f = (float)Fraction((decimal)v);
                string text = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format((decimal)v, fmt), data.YAxis.Prefix, data.YAxis.Suffix, data.YAxis.NegativeStyle);
                if (horizontalLog) canvas.DrawText(_plotLeft + f * logWidth, baselineY + 8, text, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
                else canvas.DrawText(_plotLeft - 8, baselineY - f * chartAreaHeight - AxisLabelFontSize / 2, text, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Right);
            }
            return;
        }

        if (Orientation == ChartOrientation.Horizontal)
        {
            float chartAreaWidth = Width - _plotLeft - _plotRight;
            float stepWidth = chartAreaWidth / stepCount;
            for (int i = 0; i <= stepCount; i++)
            {
                float x = _plotLeft + i * stepWidth;
                decimal value = minValue + i * stepValue;
                string label = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(value, fmt), data.YAxis.Prefix, data.YAxis.Suffix, data.YAxis.NegativeStyle);
                canvas.DrawText(x, baselineY + 8, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
            }
        }
        else
        {
            float stepHeight = chartAreaHeight / stepCount;
            for (int i = 0; i <= stepCount; i++)
            {
                float y = baselineY - i * stepHeight;
                decimal value = minValue + i * stepValue;
                string label = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(value, fmt), data.YAxis.Prefix, data.YAxis.Suffix, data.YAxis.NegativeStyle);
                canvas.DrawText(_plotLeft - 8, y - AxisLabelFontSize / 2, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Right);
            }
        }
    }

    protected virtual void DrawYAxisLegend(ICanvas canvas, AxisOptions yAxis,
        float chartAreaWidth, float chartAreaHeight, float topY, float baselineY)
    {
        string family = EffectiveFontFamily;
        if (Orientation == ChartOrientation.Horizontal)
        {
            float x = _plotLeft + chartAreaWidth / 2;
            float y = Height - 15 - (!string.IsNullOrEmpty(Caption) ? 16 : 0);
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Center);
        }
        else if (yAxis.VerticalLegend)
        {
            const float x = 15;
            float textWidth = canvas.MeasureText(yAxis.Legend, LegendFontSize, family).Width;
            float y = topY + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, 90);
        }
        else
        {
            const float x = 10;
            float y = topY + chartAreaHeight / 2;
            canvas.DrawText(x, y, yAxis.Legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Left);
        }
    }

    protected virtual void DrawXAxisLegend(ICanvas canvas, string legend,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        string family = EffectiveFontFamily;
        if (Orientation == ChartOrientation.Horizontal)
        {
            const float x = 15;
            float textWidth = canvas.MeasureText(legend, LegendFontSize, family).Width;
            float y = TitleAreaHeight + chartAreaHeight / 2 + textWidth / 2 - LegendFontSize;
            canvas.DrawTextRotated(x, y, legend, ThemeAxisColor, LegendFontSize, family, 90);
        }
        else
        {
            float x = _plotLeft + chartAreaWidth / 2;
            float labelsBottom = baselineY + 15 + AxisLabelFontSize;
            float marginBottom = baselineY + BottomPadding;
            float y = (labelsBottom + marginBottom) / 2 - LegendFontSize / 2;
            canvas.DrawText(x, y, legend, ThemeAxisColor, LegendFontSize, family, TextAlignment.Center);
        }
    }

    private static List<string> WrapLabelTwoLines(ICanvas canvas, string text, float maxWidth, float fontSize, string family)
    {
        if (canvas.MeasureText(text, fontSize, family).Width <= maxWidth) return [text];
        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            string truncated = text;
            while (truncated.Length > 1 && canvas.MeasureText(truncated + "...", fontSize, family).Width > maxWidth)
                truncated = truncated[..^1];
            return [truncated + "..."];
        }
        int bestSplit = 1;
        float bestMax = float.PositiveInfinity;
        for (int s = 1; s < words.Length; s++)
        {
            float w1 = canvas.MeasureText(string.Join(" ", words[..s]), fontSize, family).Width;
            float w2 = canvas.MeasureText(string.Join(" ", words[s..]), fontSize, family).Width;
            float m = MathF.Max(w1, w2);
            if (m < bestMax) { bestMax = m; bestSplit = s; }
        }
        return [string.Join(" ", words[..bestSplit]), string.Join(" ", words[bestSplit..])];
    }

    protected string ResolveEffectiveOverflow(ICanvas canvas, ChartData data, float chartAreaWidth)
    {
        string mode = data.XAxis.LabelOverflow;
        if (mode is not "auto") return mode;
        var pts = data.Series.Count > 0 ? data.Series[0].DataPoints : null;
        if (pts is null || pts.Count == 0) return "omit";
        float slot = chartAreaWidth / pts.Count;
        string family = EffectiveFontFamily;
        bool collides = false;
        bool allFitTwoLines = true;
        foreach (var dp in pts)
        {
            if (canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width > slot - 4)
            {
                collides = true;
                var lines = WrapLabelTwoLines(canvas, dp.Label, slot - 4, AxisLabelFontSize, family);
                if (lines.Exists(l => l.EndsWith("...", StringComparison.Ordinal) || canvas.MeasureText(l, AxisLabelFontSize, family).Width > slot - 2))
                    allFitTwoLines = false;
            }
        }
        if (!collides) return "omit";
        return allFitTwoLines ? "wrap" : "rotate";
    }

    protected virtual void DrawXAxisLabels(ICanvas canvas, ChartData data,
        float chartAreaWidth, float chartAreaHeight, float baselineY)
    {
        if (data.Series.Count == 0) return;
        if (TimeAxisActive)
        {
            DrawTimeAxisLabels(canvas, chartAreaWidth, baselineY);
            return;
        }
        ChartSeries firstSeries = data.Series[0];
        int dataPointCount = firstSeries.DataPoints.Count;
        if (dataPointCount == 0) return;
        string family = EffectiveFontFamily;

        if (Orientation == ChartOrientation.Horizontal)
        {
            float topY = baselineY - chartAreaHeight;
            float totalSpace = chartAreaHeight / dataPointCount;
            for (int i = 0; i < dataPointCount; i++)
            {
                float y = topY + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                DrawPlacedLabel(canvas, _plotLeft - 8, y - AxisLabelFontSize / 2, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Right);
            }
        }
        else
        {
            float labelY = baselineY + 15;
            float totalSpace = chartAreaWidth / dataPointCount;
            string overflow = ResolveEffectiveOverflow(canvas, data, chartAreaWidth);

            for (int i = 0; i < dataPointCount; i++)
            {
                float x = _plotLeft + i * totalSpace + totalSpace / 2;
                string label = firstSeries.DataPoints[i].Label;
                if (overflow is "rotate")
                {
                    float w = canvas.MeasureText(label, AxisLabelFontSize, family).Width;
                    float dx = w * 0.70710678f;
                    canvas.DrawTextRotated(x - dx * 0.5f, labelY + dx * 0.5f, label, ThemeAxisColor, AxisLabelFontSize, family, 45);
                }
                else if (overflow is "wrap")
                {
                    var lines = WrapLabelTwoLines(canvas, label, totalSpace - 4, AxisLabelFontSize, family);
                    for (int li = 0; li < lines.Count; li++)
                        canvas.DrawText(x, labelY + li * (AxisLabelFontSize + 2), lines[li], ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
                }
                else
                {
                    DrawPlacedLabel(canvas, x, labelY, label, ThemeAxisColor, AxisLabelFontSize, family, TextAlignment.Center);
                }
            }
        }
    }

    protected virtual Color GetDefaultSeriesColor() => Color.FromHex("#4f46e5");

    protected Color ResolveSeriesColor(ChartSeries series) =>
        series.HasColor ? series.Color : Appearance.SeriesColor(GetDefaultSeriesColor(), (_renderData ?? Data).Series.IndexOf(series));

    protected string ResolveSeriesPattern(ChartSeries series, int index)
    {
        if (!string.IsNullOrEmpty(series.Style?.Pattern)) return series.Style!.Pattern!;
        return Appearance.SeriesPatterns.Count > 0 ? Appearance.SeriesPatterns[Math.Max(0, index) % Appearance.SeriesPatterns.Count] : "none";
    }

    protected string ResolveSeriesMarkerShape(ChartSeries series, int index, string fallback = "circle")
    {
        if (!string.IsNullOrEmpty(series.Style?.MarkerShape)) return series.Style!.MarkerShape!;
        return Appearance.SeriesMarkerShapes.Count > 0 ? Appearance.SeriesMarkerShapes[Math.Max(0, index) % Appearance.SeriesMarkerShapes.Count] : fallback;
    }

    protected float[]? ResolveSeriesDashPattern(ChartSeries series, int index)
    {
        if (series.Style?.DashPattern is not null) return series.Style.DashPattern;
        return Appearance.SeriesDashPatterns.Count > 0 ? Appearance.SeriesDashPatterns[Math.Max(0, index) % Appearance.SeriesDashPatterns.Count] : null;
    }

    protected bool ReservesBottomLegendBand(ChartData data)
    {
        if (ShouldDrawLegend(data) && EffectiveLegendPosition() is "bottom") return true;
        if (Orientation == ChartOrientation.Horizontal) return !string.IsNullOrEmpty(data.YAxis.Legend);
        return !string.IsNullOrEmpty(data.XAxis.Legend);
    }

    protected (float chartAreaWidth, float chartAreaHeight, float baselineY, float topY) CalculateLayout(ChartData data)
    {
        float topBand = (!string.IsNullOrEmpty(data.Title) ? TitleAreaHeight : TopPadding) + DepthShift.Dy;
        float bottomBand = (ReservesBottomLegendBand(data) ? BottomPadding : AxisLabelBand) + DataTableHeight(data) + GetExtraBottomBand(data);

        float chartAreaWidth = Width - LeftPadding - RightPadding;
        float chartAreaHeight = Height - topBand - bottomBand;
        float baselineY = topBand + chartAreaHeight;
        float topY = topBand;
        return (chartAreaWidth, chartAreaHeight, baselineY, topY);
    }

    private (float PlotLeft, float PlotRight, float ChartAreaWidth, float ChartAreaHeight, float BaselineY, float TopY) CalculateLayoutWithCanvas(ICanvas canvas, ChartData data)
    {
        float extraRight = GetExtraRightMargin(canvas, data) + DepthShift.Dx;
        // Content sits inside the card, so every margin grows by the card inset.
        float cardInset = EffectiveCardInset;
        bool hasEnhancedLayout = AutoLayout || cardInset > 0 || !string.IsNullOrEmpty(data.Subtitle) || !string.IsNullOrEmpty(Caption)
            || LegendPosition is not "bottom" || data.XAxis.LabelOverflow is not "omit" || extraRight > 0;

        if (!hasEnhancedLayout)
        {
            var (cw, ch, by, ty) = CalculateLayout(data);
            return (LeftPadding, RightPadding, cw, ch, by, ty);
        }

        string family = EffectiveFontFamily;
        float plotLeft = LeftPadding;
        float plotRight = MathF.Max(RightPadding, extraRight);

        if (AutoLayout)
        {
            float maxLeftText = 0;
            if (Orientation == ChartOrientation.Horizontal)
            {
                if (data.Series.Count > 0)
                {
                    foreach (var dp in data.Series[0].DataPoints)
                    {
                        float w = canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width;
                        if (w > maxLeftText) maxLeftText = w;
                    }
                }
            }
            else
            {
                int steps = _effectiveStepCount > 0 ? _effectiveStepCount : 5;
                decimal stepVal = (_effectiveMax - _effectiveMin) / steps;
                for (int i = 0; i <= steps; i++)
                {
                    decimal v = _effectiveMin + i * stepVal;
                    string lbl = ChartNumberFormat.ApplyAffixes(ChartNumberFormat.Format(v, _effectiveTickFormat), data.YAxis.Prefix, data.YAxis.Suffix, data.YAxis.NegativeStyle);
                    float w = canvas.MeasureText(lbl, AxisLabelFontSize, family).Width;
                    if (w > maxLeftText) maxLeftText = w;
                }
            }
            bool hasLeftLegend = Orientation == ChartOrientation.Horizontal ? !string.IsNullOrEmpty(data.XAxis.Legend) : !string.IsNullOrEmpty(data.YAxis.Legend);
            plotLeft = MathF.Max(24, MathF.Min(Width * 0.35f, MathF.Ceiling(maxLeftText + 16 + (hasLeftLegend ? 24 : 0))));
            plotRight = MathF.Max(20, extraRight);
            if (Orientation == ChartOrientation.Horizontal && ShowValueLabels)
            {
                float maxValW = 0;
                foreach (var s in data.Series)
                    foreach (var dp in s.DataPoints)
                    {
                        float w = canvas.MeasureText(FormatPointValue(dp), LabelFontSize, family).Width;
                        if (w > maxValW) maxValW = w;
                    }
                plotRight = MathF.Max(plotRight, MathF.Min(Width * 0.25f, MathF.Ceiling(maxValW + 20)));
            }
        }

        string legendPos = ShouldDrawLegend(data) ? EffectiveLegendPosition() : "none";
        if (legendPos is "right" or "inline")
        {
            float maxNameW = 0;
            foreach (var s in data.Series)
            {
                float w = canvas.MeasureText(s.Name, 11, family).Width;
                if (w > maxNameW) maxNameW = w;
            }
            float needed = MathF.Min(Width * 0.35f, MathF.Ceiling(maxNameW + (legendPos is "right" ? 40 : 20)));
            plotRight = MathF.Max(plotRight, needed);
        }

        float topBand = (!string.IsNullOrEmpty(data.Title) ? TitleAreaHeight : TopPadding) + DepthShift.Dy;
        if (!string.IsNullOrEmpty(data.Subtitle)) topBand += !string.IsNullOrEmpty(data.Title) ? 18 : 24;
        if (legendPos is "top") topBand += 24;

        float bottomBand = ReservesBottomLegendBand(data) ? BottomPadding : AxisLabelBand;
        if (!string.IsNullOrEmpty(Caption)) bottomBand += 16;
        bottomBand += DataTableHeight(data) + GetExtraBottomBand(data);

        float prelimW = MathF.Max(40, Width - plotLeft - plotRight);
        if (Orientation == ChartOrientation.Vertical && data.XAxis.LabelOverflow is not "omit")
        {
            string eff = ResolveEffectiveOverflow(canvas, data, prelimW);
            if (eff is "rotate")
            {
                float maxLbl = 0;
                if (data.Series.Count > 0)
                    foreach (var dp in data.Series[0].DataPoints)
                    {
                        float w = canvas.MeasureText(dp.Label, AxisLabelFontSize, family).Width;
                        if (w > maxLbl) maxLbl = w;
                    }
                bottomBand += MathF.Min(60, MathF.Ceiling(maxLbl * 0.70710678f * 0.65f));
            }
            else if (eff is "wrap")
            {
                bottomBand += 14;
            }
        }

        plotLeft += cardInset;
        plotRight += cardInset;
        topBand += cardInset;
        bottomBand += cardInset;

        float maxHorizMargin = Width * 0.6f;
        if (plotLeft + plotRight > maxHorizMargin)
        {
            float scale = maxHorizMargin / (plotLeft + plotRight);
            plotLeft *= scale;
            plotRight *= scale;
        }
        float maxVertMargin = Height * 0.6f;
        if (topBand + bottomBand > maxVertMargin)
        {
            float scale = maxVertMargin / (topBand + bottomBand);
            topBand *= scale;
            bottomBand *= scale;
        }

        float cw2 = Width - plotLeft - plotRight;
        float ch2 = Height - topBand - bottomBand;
        return (plotLeft, plotRight, cw2, ch2, topBand + ch2, topBand);
    }
}
