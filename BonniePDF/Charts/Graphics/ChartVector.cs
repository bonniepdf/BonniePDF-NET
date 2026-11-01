// Generated chart source. Do not edit here; the source manifest records its provenance.
using BonniePDF.Drawing;
namespace BonniePDF;
internal static class ChartVector
{
    internal static void Fill(ICanvas sink, ChartPathPlan plan)
    {
        sink.SaveState();
        try
        {
            sink.BeginPath();
            foreach(var c in plan.Commands){var v=c.Values;switch(c.Op){
                case "M":sink.MoveTo((float)v[0],(float)v[1]);break;
                case "L":sink.LineTo((float)v[0],(float)v[1]);break;
                case "C":sink.CubicBezierTo((float)v[0],(float)v[1],(float)v[2],(float)v[3],(float)v[4],(float)v[5]);break;
                case "Z":sink.ClosePath();break;
            }}
            if(plan.Gradient is {} g)
                sink.FillCurrentPath(new LinearGradientPaint(new Point((float)g.StartX,(float)g.StartY),new Point((float)g.EndX,(float)g.EndY),[new GradientStop(0,g.StartColor),new GradientStop(1,g.EndColor)]));
            else sink.FillCurrentPath(plan.Color);
        }
        finally {sink.RestoreState();}
    }
}
