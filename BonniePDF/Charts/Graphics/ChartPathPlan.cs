// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
namespace BonniePDF;

internal sealed record ChartPathCommand(string Op, double[] Values);
internal sealed record ChartGradientStop(double Offset, Color Color);
internal sealed record ChartResolvedGradient(double StartX, double StartY, double EndX, double EndY, Color StartColor, Color EndColor, IReadOnlyList<ChartGradientStop>? Stops = null);
internal sealed record ChartPathPlan(IReadOnlyList<ChartPathCommand> Commands, Color Color, ChartResolvedGradient? Gradient);
internal sealed record ChartStrokePlan(
    IReadOnlyList<ChartPathCommand> Commands,
    Color Color,
    double LineWidth,
    string LineCap = "butt",
    string LineJoin = "miter",
    double MiterLimit = 10,
    IReadOnlyList<double>? DashPattern = null,
    double DashOffset = 0);
internal sealed record ChartPatternFill(
    string Kind,
    Color Foreground,
    Color? Background = null,
    double Spacing = 6,
    double StrokeWidth = 1);
