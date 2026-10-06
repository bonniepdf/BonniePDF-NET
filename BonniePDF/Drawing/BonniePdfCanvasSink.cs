using System;
using System.Linq;
using BonniePDF.Rendering.Svg.Sink;
using SinkFillRule = BonniePDF.Rendering.Svg.Sink.FillRule;
using SinkLineCap = BonniePDF.Rendering.Svg.Sink.LineCap;
using SinkLineJoin = BonniePDF.Rendering.Svg.Sink.LineJoin;
using SinkLinearGradientPaint = BonniePDF.Rendering.Svg.Sink.LinearGradientPaint;
using SinkPaint = BonniePDF.Rendering.Svg.Sink.Paint;
using SinkPaintTransform = BonniePDF.Rendering.Svg.Sink.PaintTransform;
using SinkRadialGradientPaint = BonniePDF.Rendering.Svg.Sink.RadialGradientPaint;
using SinkSolidPaint = BonniePDF.Rendering.Svg.Sink.SolidPaint;
using SinkStrokeStyle = BonniePDF.Rendering.Svg.Sink.StrokeStyle;

namespace BonniePDF.Drawing;

/// <summary>
/// Draws what the SVG importer emits onto an <see cref="ICanvas"/>.
/// </summary>
/// <remarks>
/// The importer's sink mirrors <see cref="ICanvas"/> member for member, so this
/// forwards 1:1 and converts only the value types, which the importer keeps as its
/// own so that it stays independent of BonniePDF's.
/// </remarks>
internal sealed class BonniePdfCanvasSink : IVectorSink
{
    /// <summary>
    /// SVG's initial <c>stroke-miterlimit</c>. The importer passes a miter limit only
    /// when the document sets one; an SVG stroke without one still has SVG's default,
    /// not PDF's (10).
    /// </summary>
    private const float SvgDefaultMiterLimit = 4f;

    private readonly ICanvas _canvas;
    private readonly Func<byte[], ImageInfo> _decodeImage;

    /// <summary>Creates a sink drawing onto <paramref name="canvas"/>.</summary>
    /// <param name="canvas">The surface to draw on.</param>
    /// <param name="decodeImage">
    /// Decodes a rasterized fallback image; defaults to <see cref="ImageLoader.Load(byte[])"/>.
    /// </param>
    public BonniePdfCanvasSink(ICanvas canvas, Func<byte[], ImageInfo>? decodeImage = null)
    {
        _canvas = canvas;
        _decodeImage = decodeImage ?? ImageLoader.Load;
    }

    public void SaveState() => _canvas.SaveState();

    public void RestoreState() => _canvas.RestoreState();

    public void BeginPath() => _canvas.BeginPath();

    public void MoveTo(float x, float y) => _canvas.MoveTo(x, y);

    public void LineTo(float x, float y) => _canvas.LineTo(x, y);

    public void CubicBezierTo(float cp1x, float cp1y, float cp2x, float cp2y, float x, float y) =>
        _canvas.CubicBezierTo(cp1x, cp1y, cp2x, cp2y, x, y);

    public void QuadraticBezierTo(float cpx, float cpy, float x, float y) => _canvas.QuadraticBezierTo(cpx, cpy, x, y);

    public void AddRect(float x, float y, float width, float height) => _canvas.AddRect(x, y, width, height);

    public void AddEllipse(float cx, float cy, float rx, float ry) => _canvas.AddEllipse(cx, cy, rx, ry);

    public void AddCircle(float cx, float cy, float radius) => _canvas.AddCircle(cx, cy, radius);

    public void ClosePath() => _canvas.ClosePath();

    public void FillCurrentPath(SinkPaint paint, SinkFillRule fillRule = SinkFillRule.NonZero) =>
        _canvas.FillCurrentPath(ToPaint(paint), ToFillRule(fillRule));

    public void StrokeCurrentPath(SinkStrokeStyle style) => _canvas.StrokeCurrentPath(new StrokeStyle
    {
        Paint = ToPaint(style.Paint),
        Width = style.Width,
        MiterLimit = style.MiterLimit ?? SvgDefaultMiterLimit,
        DashPattern = style.DashPattern,
        DashOffset = style.DashOffset ?? 0f,
        LineCap = style.LineCap switch
        {
            SinkLineCap.Round => LineCap.Round,
            SinkLineCap.Square => LineCap.Square,
            _ => LineCap.Butt,
        },
        LineJoin = style.LineJoin switch
        {
            SinkLineJoin.Round => LineJoin.Round,
            SinkLineJoin.Bevel => LineJoin.Bevel,
            _ => LineJoin.Miter,
        },
    });

    public void Translate(float dx, float dy) => _canvas.Translate(dx, dy);

    public void Scale(float sx, float sy) => _canvas.Scale(sx, sy);

    public void Rotate(float angleDegrees) => _canvas.Rotate(angleDegrees);

    public void Transform(float a, float b, float c, float d, float tx, float ty) => _canvas.Transform(a, b, c, d, tx, ty);

    public void ClipToCurrentPath(SinkFillRule fillRule = SinkFillRule.NonZero) => _canvas.ClipToCurrentPath(ToFillRule(fillRule));

    public void SetGlobalAlpha(float alpha) => _canvas.SetGlobalAlpha(alpha);

    public void DrawImage(SinkImage image, float x, float y, float width, float height) =>
        _canvas.DrawImage(_decodeImage(image.Png), x, y, width, height);

    private static FillRule ToFillRule(SinkFillRule rule) => rule == SinkFillRule.EvenOdd ? FillRule.EvenOdd : FillRule.NonZero;

    private static Color ToColor(SinkColor color) => Color.FromScRgb(color.R, color.G, color.B, color.A);

    private static Point ToPoint(SinkPoint point) => new(point.X, point.Y);

    private static PaintTransform? ToTransform(SinkPaintTransform? transform) =>
        transform is { } t ? new PaintTransform(t.A, t.B, t.C, t.D, t.E, t.F) : null;

    private static GradientStop[] ToStops(Rendering.Svg.Sink.GradientStop[] stops) =>
        stops.Select(stop => new GradientStop(stop.Offset, ToColor(stop.Color))).ToArray();

    private static Paint ToPaint(SinkPaint paint) => paint switch
    {
        SinkSolidPaint solid => new SolidPaint(ToColor(solid.Color)),
        SinkLinearGradientPaint linear => new LinearGradientPaint(ToPoint(linear.Start), ToPoint(linear.End), ToStops(linear.Stops))
        {
            Transform = ToTransform(linear.Transform),
        },
        SinkRadialGradientPaint radial => new RadialGradientPaint(
            ToPoint(radial.Center),
            radial.Radius,
            ToStops(radial.Stops),
            radial.Focal is { } focal ? ToPoint(focal) : null)
        {
            Transform = ToTransform(radial.Transform),
        },
        _ => throw new ArgumentOutOfRangeException(nameof(paint), paint.GetType().Name, "unknown paint kind"),
    };
}
