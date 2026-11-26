// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
namespace BonniePDF;

internal sealed record ChartPathCommand(string Op, double[] Values);
internal sealed record ChartGradientStop(double Offset, Color Color);
internal sealed record ChartResolvedGradient(double StartX, double StartY, double EndX, double EndY, Color StartColor, Color EndColor, IReadOnlyList<ChartGradientStop>? Stops = null);
/// <summary>A resolved visual effect attached to a plan: <c>shadow</c> (blur, offsets), <c>glow</c> (radius in <c>Blur</c>) or <c>gloss</c> (extent).</summary>
internal sealed record ChartResolvedEffect(string Kind, Color? Color, double Opacity, double Blur = 0, double OffsetX = 0, double OffsetY = 0, double Extent = 0)
{
    internal bool IsShadow => string.Equals(Kind, "shadow", System.StringComparison.Ordinal);
    internal bool IsGlow => string.Equals(Kind, "glow", System.StringComparison.Ordinal);
    internal bool IsGloss => string.Equals(Kind, "gloss", System.StringComparison.Ordinal);
}
internal sealed record ChartResolvedRadialGradient(double Cx, double Cy, double R, double Fx, double Fy, IReadOnlyList<ChartGradientStop> Stops);
internal sealed record ChartPathPlan(IReadOnlyList<ChartPathCommand> Commands, Color Color, ChartResolvedGradient? Gradient, ChartResolvedRadialGradient? RadialGradient = null, IReadOnlyList<ChartResolvedEffect>? Effects = null);
internal sealed record ChartStrokePlan(
    IReadOnlyList<ChartPathCommand> Commands,
    Color Color,
    double LineWidth,
    string LineCap = "butt",
    string LineJoin = "miter",
    double MiterLimit = 10,
    IReadOnlyList<double>? DashPattern = null,
    double DashOffset = 0,
    IReadOnlyList<ChartResolvedEffect>? Effects = null);
internal sealed record ChartPatternFill(
    string Kind,
    Color Foreground,
    Color? Background = null,
    double Spacing = 6,
    double StrokeWidth = 1);
