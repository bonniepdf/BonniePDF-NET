// Generated chart source. Do not edit here; the source manifest records its provenance.
using System.Collections.Generic;
namespace BonniePDF;

internal sealed record ChartPathCommand(string Op, double[] Values);
internal sealed record ChartResolvedGradient(double StartX, double StartY, double EndX, double EndY, Color StartColor, Color EndColor);
internal sealed record ChartPathPlan(IReadOnlyList<ChartPathCommand> Commands, Color Color, ChartResolvedGradient? Gradient);
