// Generated chart source. Do not edit here; the source manifest records its provenance.
namespace BonniePDF;

/// <summary>Where a mark grows from when it animates in, in chart coordinates.</summary>
/// <param name="X">Horizontal position.</param>
/// <param name="Y">Vertical position.</param>
/// <param name="Axis">The mark scales along <c>x</c> (horizontal bars), along <c>y</c> (vertical bars), or in <c>both</c> from the anchor (slices, points).</param>
public sealed record ChartGroupAnchor(double X, double Y, string Axis);

/// <summary>Describes a group of marks for sinks that keep chart structure. Charts emit the <c>series</c> and <c>mark</c> roles.</summary>
/// <param name="Role">What the group contains: <c>chart</c>, <c>background</c>, <c>plot</c>, <c>grid</c>, <c>axis</c>, <c>series</c>, <c>mark</c>, <c>label</c>, <c>annotation</c>, <c>legend</c> or <c>title</c>.</param>
/// <param name="SeriesIndex">Set for <c>series</c> and <c>mark</c>.</param>
/// <param name="PointIndex">Set for <c>mark</c>.</param>
/// <param name="Name">A human-readable name: the series name, or the category label.</param>
/// <param name="Description">Accessible text for the mark, built by the chart from <c>MarkDescriptionTemplate</c>.</param>
/// <param name="Anchor">Where the mark grows from when it animates in.</param>
public sealed record ChartGroupInfo(string Role, int? SeriesIndex = null, int? PointIndex = null, string? Name = null, string? Description = null, ChartGroupAnchor? Anchor = null);
