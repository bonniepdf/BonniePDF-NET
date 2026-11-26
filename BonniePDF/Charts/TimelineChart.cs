// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>One dated event of a <see cref="TimelineChart"/>.</summary>
public sealed class TimelineEvent
{
    /// <summary>When it happened (UTC).</summary>
    public required DateTimeOffset Date { get; set; }
    /// <summary>The event's name.</summary>
    public required string Label { get; set; }
    /// <summary>A second line of text under the date.</summary>
    public string? Detail { get; set; }
    /// <summary>Colour of the event's dot, stem and label. Unset uses the next colour of the theme.</summary>
    public Color? Color { get; set; }
}

/// <summary>A labelled span of time drawn behind the axis of a <see cref="TimelineChart"/>.</summary>
public sealed class TimelineEra
{
    /// <summary>When the span starts (UTC).</summary>
    public required DateTimeOffset Start { get; set; }
    /// <summary>When the span ends (UTC).</summary>
    public required DateTimeOffset End { get; set; }
    /// <summary>The span's label.</summary>
    public required string Label { get; set; }
    /// <summary>The span's colour.</summary>
    public required Color Fill { get; set; }
}

/// <summary>Events on a horizontal axis. Labels alternate above and below, stepping outwards to keep clear of one another.</summary>
public class TimelineChart : ChartBase
{
    private const int MaxLevels = 4;
    private int _omitted;

    /// <summary>The events.</summary>
    public IList<TimelineEvent> Events { get; set; } = [];
    /// <summary>Spans drawn behind the axis.</summary>
    public IList<TimelineEra> Eras { get; set; } = [];
    /// <summary>Format of each event's date line: <c>yyyy</c>, <c>yy</c>, <c>Q</c>, <c>MMM</c>, <c>MM</c>, <c>dd</c>, <c>d</c>, <c>HH</c>, <c>mm</c> and <c>'literal'</c>.</summary>
    public string DateFormat { get; set; } = "dd MMM yyyy";

    /// <summary>Events the last draw left out because no label position was free.</summary>
    public int OmittedCount => _omitted;

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty timeline.</summary>
    public TimelineChart() { AutoLayout = true; }

    /// <summary>Creates a timeline for <paramref name="data"/>.</summary>
    public TimelineChart(ChartData data) : base(data) { AutoLayout = true; }

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        _omitted = 0;
        if (Events.Count == 0) return;
        var events = Events.Select((e, i) => (E: e, I: i, T: ChartDates.Parse(e.Date))).OrderBy(x => x.T).ThenBy(x => x.I).ToList();
        var eras = Eras.Select(e => (E: e, Start: ChartDates.Parse(e.Start), End: ChartDates.Parse(e.End))).ToList();
        foreach (var era in eras) if (era.End < era.Start) throw new InvalidOperationException($"Era '{era.E.Label}' ends before it starts.");
        List<long> times = events.Select(x => x.T).Concat(eras.SelectMany(x => new[] { x.Start, x.End })).ToList();
        long min = times.Min(), max = times.Max();
        if (!(max > min)) { min -= 86_400_000; max += 86_400_000; }
        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        const float left = 40;
        float right = Width - 40;
        float top = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        float bottom = Height - 14 - (Caption.Length > 0 ? 16 : 0);
        float axisY = (top + bottom) / 2;
        float XOf(long t) => left + (float)((double)(t - min) / (max - min) * (right - left));

        foreach (var era in eras)
        {
            float x = XOf(era.Start);
            float w = Math.Max(1, XOf(era.End) - x);
            canvas.FillRect(x, axisY - 11, w, 22, era.E.Fill);
            canvas.DrawText(x + 4, axisY - 10, era.E.Label, Appearance.LegendTextColor, 9, font, TextAlignment.Left);
        }
        canvas.DrawLine(left, axisY, right, axisY, Appearance.AxisColor, 2);

        var blocks = events.Select(x =>
        {
            List<string> lines = [x.E.Label, ChartDates.Format(x.T, DateFormat)];
            if (!string.IsNullOrEmpty(x.E.Detail)) lines.Add(x.E.Detail);
            float width = 0;
            for (int k = 0; k < lines.Count; k++) width = Math.Max(width, canvas.MeasureText(lines[k], k == 0 ? 10 : 9, k == 0 ? boldFont : font).Width);
            return (x.E, x.I, x.T, Lines: lines, Width: width);
        }).ToList();
        const float lineHeight = 12;
        float maxHeight = blocks.Max(b => b.Lines.Count) * lineHeight;
        float step = maxHeight + 6;
        float[][] rightEdge = [Enumerable.Repeat(float.NegativeInfinity, MaxLevels).ToArray(), Enumerable.Repeat(float.NegativeInfinity, MaxLevels).ToArray()];
        for (int n = 0; n < blocks.Count; n++)
        {
            var b = blocks[n];
            int side = n % 2;
            float x = XOf(b.T);
            float boxLeft = Math.Min(Math.Max(x - b.Width / 2, 4), Width - 4 - b.Width);
            int level = -1;
            for (int k = 0; k < MaxLevels; k++) if (boxLeft >= rightEdge[side][k] + 6) { level = k; break; }
            if (level < 0) { _omitted++; continue; }
            rightEdge[side][level] = boxLeft + b.Width;
            Color color = b.E.Color ?? ChartCategoryColors.For(Appearance, null, b.I);
            float height = b.Lines.Count * lineHeight;
            float offset = 18 + level * step;
            float boxTop = side == 0 ? axisY - offset - height : axisY + offset;
            float stemEnd = side == 0 ? boxTop + height + 2 : boxTop - 2;
            canvas.DrawLine(x, axisY, x, stemEnd, color, 1);
            canvas.FillCircle(x, axisY, 4, color);
            float cx = boxLeft + b.Width / 2;
            for (int k = 0; k < b.Lines.Count; k++)
                canvas.DrawText(cx, boxTop + k * lineHeight, b.Lines[k], k == 0 ? Appearance.LegendTextColor : Appearance.AxisColor, k == 0 ? 10 : 9, k == 0 ? boldFont : font, TextAlignment.Center);
        }
    }
}
