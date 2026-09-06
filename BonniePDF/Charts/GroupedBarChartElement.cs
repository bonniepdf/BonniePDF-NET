using BonniePDF.Charts.Models;
using System;

namespace BonniePDF.Charts;

/// <summary>
/// Element that renders a GroupedBarChart to the document.
/// </summary>
[Obsolete("GroupedBarChart is a document element. Set its Data and add it directly.")]
public class GroupedBarChartElement(GroupedBarChart chart, ChartData data) : ChartElement(chart, data)
{
    /// <summary>
    /// Reports no preferred width, unlike <see cref="ChartElement"/>. This wrapper has
    /// never contributed the chart's width to auto-sizing containers, and reporting one
    /// here would change how horizontal stacks and shrink-to-fit containers size an
    /// existing document.
    /// </summary>
    /// <returns>Always zero.</returns>
    protected override float GetPreferredContentWidth()
    {
        return 0;
    }
}
