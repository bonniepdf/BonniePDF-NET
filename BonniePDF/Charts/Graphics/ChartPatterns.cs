// Generated chart source. Do not edit here; the source manifest records its provenance.
using System;
using System.Collections.Generic;
using BonniePDF.Drawing;
namespace BonniePDF;

internal static class ChartPatterns
{
    internal static void FillRect(ICanvas sink, float x, float y, float width, float height, ChartPatternFill pattern)
    {
        if (width <= 0 || height <= 0 || pattern.Kind is "none") return;
        double spacing = Math.Max(2.0, pattern.Spacing);
        double strokeWidth = Math.Max(0.5, pattern.StrokeWidth);

        if (pattern.Background.HasValue)
            sink.FillRect(x, y, width, height, pattern.Background.Value);

        ChartVector.PushClipRect(sink, x, y, width, height);
        try
        {
            if (pattern.Kind is "dots")
            {
                float r = (float)Math.Max(0.5, strokeWidth);
                for (double py = y + spacing / 2.0; py < y + height + r; py += spacing)
                {
                    for (double px = x + spacing / 2.0; px < x + width + r; px += spacing)
                    {
                        sink.FillCircle((float)px, (float)py, r, pattern.Foreground);
                    }
                }
                return;
            }

            List<ChartPathCommand> cmds = new();
            if (pattern.Kind is "horizontal" or "crosshatch")
            {
                for (double py = y + spacing / 2.0; py < y + height; py += spacing)
                {
                    cmds.Add(new("M", [x, py]));
                    cmds.Add(new("L", [x + width, py]));
                }
            }
            if (pattern.Kind is "vertical" or "crosshatch")
            {
                for (double px = x + spacing / 2.0; px < x + width; px += spacing)
                {
                    cmds.Add(new("M", [px, y]));
                    cmds.Add(new("L", [px, y + height]));
                }
            }
            if (pattern.Kind is "diagonal")
            {
                double span = width + height;
                for (double offset = -height; offset <= span; offset += spacing)
                {
                    cmds.Add(new("M", [x + offset, y + height]));
                    cmds.Add(new("L", [x + offset + height, y]));
                }
            }
            if (pattern.Kind is "reverseDiagonal")
            {
                double span = width + height;
                for (double offset = -height; offset <= span; offset += spacing)
                {
                    cmds.Add(new("M", [x + offset, y]));
                    cmds.Add(new("L", [x + offset + height, y + height]));
                }
            }
            if (cmds.Count > 0)
                ChartVector.Stroke(sink, new ChartStrokePlan(cmds, pattern.Foreground, strokeWidth));
        }
        finally
        {
            ChartVector.PopClip(sink);
        }
    }
}
