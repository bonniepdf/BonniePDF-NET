// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
namespace BonniePDF;

internal static class ChartBars
{
    internal static void Draw(ICanvas sink, float x, float y, float w, float h, Color color, ChartBarStyle style, ChartTheme theme, bool horizontal, float plotX, float plotY, float plotW, float plotH)
    {
        double radius = style.CornerRadius ?? theme.BarCornerRadius;
        ChartLinearGradient? g = style.Gradient;
        if (!double.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(style), "CornerRadius must be finite and nonnegative.");
        if (g is not null && (g.StartColor.ScA != 1 || g.EndColor.ScA != 1 || g.Scope is not "bar" and not "plot")) throw new ArgumentException("Bar gradients require opaque colours and scope bar or plot.", nameof(style));
        if (!theme.StyledGeometry && radius == 0 && g is null) { sink.FillRect(x, y, w, h, color); return; }
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(w) || !float.IsFinite(h)) throw new ArgumentException("Bar geometry must be finite.");
        if (w == 0 || h == 0) return;
        double x0 = Math.Min(x, x + w), x1 = Math.Max(x, x + w), y0 = Math.Min(y, y + h), y1 = Math.Max(y, y + h);
        double r = Math.Min(radius, Math.Min((x1 - x0) / 2, (y1 - y0) / 2)), k = 0.5522847498307936;
        double tl = 0, tr = 0, br = 0, bl = 0;
        if (horizontal) { if (w > 0) tr = br = r; else tl = bl = r; } else { if (h > 0) tl = tr = r; else bl = br = r; }
        List<ChartPathCommand> commands = new();
        void Add(string op, params double[] values) => commands.Add(new(op, values));
        Add("M", x0 + tl, y0); Add("L", x1 - tr, y0);
        if (tr != 0) Add("C", x1 - tr + k * tr, y0, x1, y0 + tr - k * tr, x1, y0 + tr);
        Add("L", x1, y1 - br);
        if (br != 0) Add("C", x1, y1 - br + k * br, x1 - br + k * br, y1, x1 - br, y1);
        Add("L", x0 + bl, y1);
        if (bl != 0) Add("C", x0 + bl - k * bl, y1, x0, y1 - bl + k * bl, x0, y1 - bl);
        Add("L", x0, y0 + tl);
        if (tl != 0) Add("C", x0, y0 + tl - k * tl, x0 + tl - k * tl, y0, x0 + tl, y0);
        Add("Z");
        ChartResolvedGradient? gradient = null;
        if (g is not null)
        {
            double px = g.Scope == "plot" ? plotX : x0, py = g.Scope == "plot" ? plotY : y0, pw = g.Scope == "plot" ? plotW : x1 - x0, ph = g.Scope == "plot" ? plotH : y1 - y0;
            double sx = horizontal ? px + pw : px, sy = py, ex = px, ey = horizontal ? py : py + ph;
            if (g.Scope == "bar" && ((horizontal && w < 0) || (!horizontal && h < 0))) { (sx, ex) = (ex, sx); (sy, ey) = (ey, sy); }
            gradient = new(sx, sy, ex, ey, g.StartColor, g.EndColor);
        }
        ChartVector.Fill(sink, new ChartPathPlan(commands, color, gradient));
    }
}
