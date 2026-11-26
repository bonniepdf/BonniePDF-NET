// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using System.Linq;
using BonniePDF.Drawing;

namespace BonniePDF;

/// <summary>One task of a <see cref="GanttChart"/>.</summary>
public sealed class GanttTask
{
    /// <summary>Unique id, referenced by <see cref="DependsOn"/>.</summary>
    public required string Id { get; set; }
    /// <summary>The row label.</summary>
    public required string Label { get; set; }
    /// <summary>When the task starts (UTC).</summary>
    public required DateTimeOffset Start { get; set; }
    /// <summary>When it ends. A task with no end is a milestone.</summary>
    public DateTimeOffset? End { get; set; }
    /// <summary>The finished fraction, <c>0..1</c>: the leading part of the bar is drawn in the full colour, the rest in a tint.</summary>
    public double? Progress { get; set; }
    /// <summary>Consecutive tasks with the same group sit under a bold group row.</summary>
    public string? Group { get; set; }
    /// <summary>Bar colour. Unset uses the next colour of the theme for the group, or for the task when it has none.</summary>
    public Color? Color { get; set; }
    /// <summary>Draw a diamond at <see cref="Start"/> instead of a bar.</summary>
    public bool Milestone { get; set; }
    /// <summary>Ids of tasks that must finish first: an elbow arrow runs from each one's end to this task's start.</summary>
    public IList<string>? DependsOn { get; set; }
}

/// <summary>A Gantt chart: tasks as bars on a time axis, with groups, progress, milestones, dependencies and a today marker.</summary>
public class GanttChart : ChartBase
{
    /// <summary>The tasks, one row each, in order.</summary>
    public IList<GanttTask> Tasks { get; set; } = [];
    /// <summary>The time axis.</summary>
    public ChartTimeAxis TimeAxis { get; set; } = new();
    /// <summary>Draws a labelled vertical line at this date.</summary>
    public DateTimeOffset? TodayDate { get; set; }
    /// <summary>Height of a row in points (<c>22</c>); rows shrink to fit the chart height.</summary>
    public float RowHeight { get; set; } = 22;
    /// <summary>The fraction of a row a bar fills (<c>0.6</c>).</summary>
    public float BarHeightRatio { get; set; } = 0.6f;
    /// <summary>Width of the label column in points. Unset uses the widest label, at most 35 % of the chart width.</summary>
    public float? LabelColumnWidth { get; set; }
    /// <summary>Write the progress percentage inside bars that are wide enough.</summary>
    public bool ShowProgressLabels { get; set; }

    internal override bool UsesStyledGeometry => true;

    /// <summary>Creates an empty Gantt chart.</summary>
    public GanttChart() { AutoLayout = true; }

    /// <summary>Creates a Gantt chart for <paramref name="data"/>.</summary>
    public GanttChart(ChartData data) : base(data) { AutoLayout = true; }

    private sealed record Resolved(GanttTask Task, long Start, long End, bool Milestone);
    private sealed record Row(bool IsGroup, string Label, int Index, string? Group);

    /// <inheritdoc />
    protected override void DrawContent(ICanvas canvas, ChartData data)
    {
        if (Tasks.Count == 0) return;
        if (!(RowHeight > 0)) throw new InvalidOperationException("Gantt rowHeight must be positive.");
        if (!(BarHeightRatio > 0 && BarHeightRatio <= 1)) throw new InvalidOperationException("Gantt barHeightRatio must be above 0 and at most 1.");
        HashSet<string> ids = new(StringComparer.Ordinal);
        List<Resolved> resolved = [];
        foreach (GanttTask t in Tasks)
        {
            if (!ids.Add(t.Id)) throw new InvalidOperationException($"Duplicate task id '{t.Id}'.");
            long start = ChartDates.Parse(t.Start);
            bool milestone = t.Milestone || t.End is null;
            long end = t.End is { } e ? ChartDates.Parse(e) : start;
            if (end < start) throw new InvalidOperationException($"Task '{t.Id}' ends before it starts.");
            if (t.Progress is { } p && !(p >= 0 && p <= 1)) throw new InvalidOperationException($"Task '{t.Id}' progress must be from 0 to 1.");
            resolved.Add(new Resolved(t, start, end, milestone));
        }
        Dictionary<string, int> byId = new(StringComparer.Ordinal);
        for (int i = 0; i < resolved.Count; i++) byId[resolved[i].Task.Id] = i;
        foreach (Resolved r in resolved)
            foreach (string dep in r.Task.DependsOn ?? [])
                if (!byId.ContainsKey(dep)) throw new InvalidOperationException($"Task '{r.Task.Id}' depends on unknown task '{dep}'.");

        List<Row> rows = [];
        for (int i = 0; i < resolved.Count; i++)
        {
            string? g = resolved[i].Task.Group;
            if (g is not null && (i == 0 || !string.Equals(resolved[i - 1].Task.Group, g, StringComparison.Ordinal))) rows.Add(new Row(true, g, i, g));
            rows.Add(new Row(false, resolved[i].Task.Label, i, g));
        }
        List<string> groups = [];
        foreach (Resolved r in resolved) if (r.Task.Group is { } g && !groups.Contains(g, StringComparer.Ordinal)) groups.Add(g);

        string font = ResolveFontFamily();
        string boldFont = string.Equals(font, StandardFonts.Helvetica, StringComparison.Ordinal) ? StandardFonts.HelveticaBold : font;
        ChartTimeAxis axis = TimeAxis;
        long min = axis.MinDate is { } mn ? ChartDates.Parse(mn) : resolved.Min(r => r.Start);
        long max = axis.MaxDate is { } mx ? ChartDates.Parse(mx) : resolved.Max(r => r.End);
        if (!(max > min)) max = min + 86_400_000;
        long? today = TodayDate is { } td ? ChartDates.Parse(td) : null;

        float widest = 0;
        foreach (Row row in rows) widest = Math.Max(widest, canvas.MeasureText(row.Label, 10, row.IsGroup ? boldFont : font).Width + (!row.IsGroup && row.Group is not null ? 10 : 0));
        float labelColumn = LabelColumnWidth ?? Math.Min(widest + 16, Width * 0.35f);
        float x0 = 12 + labelColumn;
        float x1 = Width - 24;
        float top = (data.Title.Length > 0 ? 56 : 18) + (data.Subtitle.Length > 0 ? 18 : 0);
        const float headerBand = 22;
        float bottom = Height - 14 - (Caption.Length > 0 ? 16 : 0);
        float rowsTop = top + headerBand;
        float rowH = Math.Min(RowHeight, (bottom - rowsTop) / rows.Count);
        float XOf(long t) => x0 + (float)((double)(t - min) / (max - min) * (x1 - x0));
        string unit = string.Equals(axis.Unit, "auto", StringComparison.Ordinal) ? ChartDates.ChooseUnit(min, max, x1 - x0) : axis.Unit;
        string format = axis.Format ?? ChartDates.DefaultFormat(unit);

        foreach (long t in ChartDates.Ticks(min, max, unit))
        {
            float x = XOf(t);
            if (axis.ShowGridLines) canvas.DrawLine(x, rowsTop, x, rowsTop + rowH * rows.Count, Appearance.GridLineColor, 0.5f);
            canvas.DrawText(x, top + 4, ChartDates.Format(t, format), Appearance.AxisColor, 10, font, TextAlignment.Center);
        }

        Dictionary<int, float> rowCentre = [];
        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            float y = rowsTop + i * rowH;
            float cy = y + rowH / 2;
            if (!row.IsGroup) rowCentre[row.Index] = cy;
            float indent = !row.IsGroup && row.Group is not null ? 10 : 0;
            canvas.DrawText(12 + indent, cy - 5, row.Label, Appearance.LegendTextColor, 10, row.IsGroup ? boldFont : font, TextAlignment.Left);
            float barH = rowH * BarHeightRatio;
            if (row.IsGroup)
            {
                List<Resolved> members = resolved.Where(r => string.Equals(r.Task.Group, row.Group, StringComparison.Ordinal)).ToList();
                long gs = members.Min(m => m.Start), ge = members.Max(m => m.End);
                canvas.FillRect(XOf(gs), cy - 3, Math.Max(1, XOf(ge) - XOf(gs)), 6, Appearance.AxisColor);
                continue;
            }
            Resolved r2 = resolved[row.Index];
            int colorIndex = r2.Task.Group is { } grp ? groups.IndexOf(grp) : row.Index;
            Color color = r2.Task.Color ?? ChartCategoryColors.For(Appearance, null, colorIndex);
            if (r2.Milestone)
            {
                float cx = XOf(r2.Start);
                float h = rowH * 0.35f;
                ChartVector.Fill(canvas, new ChartPathPlan([new("M", [cx, cy - h]), new("L", [cx + h, cy]), new("L", [cx, cy + h]), new("L", [cx - h, cy]), new("Z", [])], color, null));
                continue;
            }
            float bx = XOf(r2.Start);
            float bw = Math.Max(1, XOf(r2.End) - bx);
            float by = cy - barH / 2;
            if (r2.Task.Progress is not { } progress) canvas.FillRect(bx, by, bw, barH, color);
            else
            {
                canvas.FillRect(bx, by, bw, barH, ChartTint.Tint(color));
                if (progress > 0) canvas.FillRect(bx, by, (float)(bw * progress), barH, color);
                if (ShowProgressLabels)
                {
                    string text = ((int)Math.Round(progress * 100, MidpointRounding.AwayFromZero)).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%";
                    if (canvas.MeasureText(text, 9, font).Width + 6 <= bw)
                        canvas.DrawText(bx + bw / 2, cy - 4.5f, text, ChartValueScale.ContrastTextColor(progress >= 0.5 ? color : ChartTint.Tint(color)), 9, font, TextAlignment.Center);
                }
            }
        }

        Color arrow = Appearance.AxisColor;
        for (int i = 0; i < resolved.Count; i++)
        {
            foreach (string dep in resolved[i].Task.DependsOn ?? [])
            {
                int pi = byId[dep];
                float px = XOf(resolved[pi].End), py = rowCentre[pi];
                float sx = XOf(resolved[i].Start), sy = rowCentre[i];
                ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [px, py]), new("L", [px + 8, py]), new("L", [px + 8, sy]), new("L", [sx - 5, sy])], arrow, 1, "butt", "miter", 10));
                ChartVector.Fill(canvas, new ChartPathPlan([new("M", [sx, sy]), new("L", [sx - 5, sy - 3]), new("L", [sx - 5, sy + 3]), new("Z", [])], arrow, null));
            }
        }

        if (today is { } now && now >= min && now <= max)
        {
            float x = XOf(now);
            Color color = Appearance.NegativeColor ?? Color.FromHex("#b91c1c");
            ChartVector.Stroke(canvas, new ChartStrokePlan([new("M", [x, rowsTop]), new("L", [x, rowsTop + rowH * rows.Count])], color, 1.25, "butt", "miter", 10, [4, 3]));
            canvas.DrawText(x, rowsTop + rowH * rows.Count + 3, "Today", color, 9, font, TextAlignment.Center);
        }
    }
}
