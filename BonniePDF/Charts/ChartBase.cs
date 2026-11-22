// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>
/// Base class for all charts that provides common functionality for drawing charts
/// using the ICanvas interface.
/// </summary>
public abstract partial class ChartBase
{
    private const double RoundedK = 0.5522847498307933;
    private string _theme = "legacy";
    private ChartThemeDefinition? _customTheme;
    private ChartTheme _resolvedTheme = ChartTheme.Legacy;
    private float _borderRadius;
    private float _borderWidth;

    /// <summary>Ambient document default font family supplied by a PDF host when <see cref="InheritDocumentFont"/> is true.</summary>
    protected string? InheritedFontFamily { get; set; }

    /// <summary>Appearance preset: "legacy" (default), "modern", "dark", or "print".</summary>
    public string Theme
    {
        get => _theme;
        set
        {
            if (!ChartTheme.IsValidPreset(value)) throw new ArgumentException("Invalid chart theme.", nameof(value));
            _theme = value;
            _resolvedTheme = ChartTheme.Resolve(_theme, _customTheme);
        }
    }

    /// <summary>Optional composable theme overrides applied on top of a built-in preset.</summary>
    public ChartThemeDefinition? CustomTheme
    {
        get => _customTheme;
        set
        {
            _customTheme = value;
            _resolvedTheme = ChartTheme.Resolve(_theme, _customTheme);
        }
    }

    /// <summary>The values and behaviours behind <see cref="Theme"/> and <see cref="CustomTheme"/>.</summary>
    internal ChartTheme Appearance => _resolvedTheme;
    internal virtual bool UsesStyledGeometry => Appearance.StyledGeometry || _borderRadius > 0 || (BorderColor.HasValue && _borderWidth > 0);

    protected ChartBase() { }

    protected ChartBase(ChartData data)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
    }

    private ChartData _data = new();

    public ChartData Data
    {
        get => _data;
        set => _data = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string Title
    {
        get => Data.Title;
        set => Data.Title = value;
    }

    /// <summary>Optional subtitle drawn beneath the chart title.</summary>
    public string Subtitle
    {
        get => Data.Subtitle;
        set => Data.Subtitle = value;
    }

    /// <summary>Optional source or footnote line drawn at the bottom-left of the chart.</summary>
    public string Caption { get; set; } = string.Empty;

    /// <summary>Enable content-aware dynamic margins based on measured text bounds.</summary>
    public bool AutoLayout { get; set; }

    /// <summary>When hosted in BonniePDF, inherit the document's default font family instead of Helvetica.</summary>
    public bool InheritDocumentFont { get; set; }

    public string AltText { get; set; } = string.Empty;
    public bool IsDecorative { get; set; }
    public float Width { get; set; } = 800;
    public float Height { get; set; } = 450;
    public Color? BackgroundColor { get; set; }

    /// <summary>Optional card border corner radius in points.</summary>
    public float BorderRadius
    {
        get => _borderRadius;
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), "BorderRadius must be finite and nonnegative.");
            _borderRadius = value;
        }
    }

    /// <summary>Optional card border colour.</summary>
    public Color? BorderColor { get; set; }

    /// <summary>Optional card border stroke width in points.</summary>
    public float BorderWidth
    {
        get => _borderWidth;
        set
        {
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value), "BorderWidth must be finite and nonnegative.");
            _borderWidth = value;
        }
    }

    public string? SvgShapeRendering { get; set; }

    protected Color? EffectiveBackgroundColor => BackgroundColor ?? Appearance.BackgroundColor;

    protected string ResolveFontFamily(string? explicitFamily = null)
    {
        ChartTheme t = Appearance;
        if ((InheritDocumentFont || string.Equals(t.FontFamily, "inherit", StringComparison.Ordinal)) && !string.IsNullOrEmpty(InheritedFontFamily))
            return InheritedFontFamily;
        if (!string.IsNullOrEmpty(explicitFamily) && !string.Equals(explicitFamily, StandardFonts.Helvetica, StringComparison.Ordinal))
            return explicitFamily;
        return string.Equals(t.FontFamily, "inherit", StringComparison.Ordinal) ? StandardFonts.Helvetica : t.FontFamily;
    }

    protected string ResolveTitleFontFamily()
    {
        ChartTheme t = Appearance;
        if ((InheritDocumentFont || string.Equals(t.FontFamily, "inherit", StringComparison.Ordinal)) && !string.IsNullOrEmpty(InheritedFontFamily))
            return InheritedFontFamily;
        return t.TitleFontFamily;
    }

    private static List<ChartPathCommand> RoundedRectCommands(double x, double y, double w, double h, double radius)
    {
        double r = Math.Min(Math.Max(0, radius), Math.Min(w / 2.0, h / 2.0));
        if (r <= 0)
        {
            return
            [
                new("M", [x, y]),
                new("L", [x + w, y]),
                new("L", [x + w, y + h]),
                new("L", [x, y + h]),
                new("Z", []),
            ];
        }
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

    public virtual void Draw(ICanvas canvas, ChartData data)
    {
        Color? bg = EffectiveBackgroundColor;
        if (bg.HasValue)
        {
            if (_borderRadius > 0)
                ChartVector.Fill(canvas, new ChartPathPlan(RoundedRectCommands(0, 0, Width, Height, _borderRadius), bg.Value, null));
            else
                canvas.FillRect(0, 0, Width, Height, bg.Value);
        }

        if (!string.IsNullOrEmpty(data.Title)) DrawTitle(canvas, data.Title);
        if (!string.IsNullOrEmpty(data.Subtitle)) DrawSubtitle(canvas, data.Subtitle, !string.IsNullOrEmpty(data.Title));

        DrawContent(canvas, data);

        if (!string.IsNullOrEmpty(Caption)) DrawCaption(canvas, Caption);

        if (BorderColor.HasValue && _borderWidth > 0)
        {
            double inset = _borderWidth / 2.0;
            ChartVector.Stroke(canvas, new ChartStrokePlan(
                RoundedRectCommands(inset, inset, Width - _borderWidth, Height - _borderWidth, Math.Max(0, _borderRadius - inset)),
                BorderColor.Value,
                _borderWidth,
                "butt",
                "round"));
        }
    }

    public void Draw(ICanvas canvas) => Draw(canvas, Data);

    protected virtual void DrawTitle(ICanvas canvas, string title)
    {
        ChartTheme t = Appearance;
        canvas.DrawText(t.TitleX ?? Width / 2, t.TitleY, title, t.TitleColor, t.TitleFontSize, ResolveTitleFontFamily(), t.TitleAlignment);
    }

    protected virtual void DrawSubtitle(ICanvas canvas, string subtitle, bool hasTitle)
    {
        ChartTheme t = Appearance;
        float y = hasTitle ? t.TitleY + t.TitleFontSize + 4 : t.TitleY;
        canvas.DrawText(t.TitleX ?? Width / 2, y, subtitle, t.SubtitleColor, 11, ResolveFontFamily(), t.TitleAlignment);
    }

    protected virtual void DrawCaption(ICanvas canvas, string caption)
    {
        ChartTheme t = Appearance;
        canvas.DrawText(t.TitleX ?? 20, Height - 14, caption, t.SubtitleColor, 8.5f, ResolveFontFamily(), TextAlignment.Left);
    }

    protected abstract void DrawContent(ICanvas canvas, ChartData data);
}
