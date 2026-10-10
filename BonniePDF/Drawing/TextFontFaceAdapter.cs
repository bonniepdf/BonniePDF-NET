using System.Collections.Generic;
using BonniePDF.Rendering.Svg.Fonts;
using TextFace=BonniePDF.Typography.Engine.FontFace;
namespace BonniePDF.Drawing;
/// <summary>SVG font seam over the text reader.</summary>
internal sealed class TextFontFaceAdapter : IFontFace
{
    private readonly TextFace face;
    public string Family { get; }
    public int UnitsPerEm => face.UnitsPerEm;
    public TextFontFaceAdapter(byte[] bytes,string family) { face=new TextFace(bytes);Family=family; }
    public int GlyphIndex(int cp)=>face.MapGlyph(cp);
    public double AdvanceWidth(int g)=>face.AdvanceWidth(g);
    public double Kerning(int a,int b)=>face.GetPairKerning(a,b);
    public IReadOnlyList<GlyphSegment> Outline(int g)
    {
        var segments=new List<GlyphSegment>();
        foreach(var contour in face.Font.ReadGlyph(g).Contours)
        {
            if(contour.Count<2)continue;
            var points=new List<BonniePDF.Typography.Engine.FontPoint>();
            for(int i=0;i<contour.Count;i++)
            {
                var a=contour[i];var b=contour[(i+1)%contour.Count];points.Add(a);
                if(!a.OnCurve&&!b.OnCurve)points.Add(new BonniePDF.Typography.Engine.FontPoint((a.X+b.X)/2,(a.Y+b.Y)/2,true));
            }
            int start=points.FindIndex(p=>p.OnCurve);if(start<0)continue;
            BonniePDF.Typography.Engine.FontPoint At(int i)=>points[(start+i)%points.Count];
            segments.Add(new GlyphSegment(GlyphSegmentKind.Move,At(0).X,At(0).Y));
            for(int i=1;i<points.Count;i++)
            {
                var p=At(i);
                if(p.OnCurve)segments.Add(new GlyphSegment(GlyphSegmentKind.Line,p.X,p.Y));
                else {var end=At(++i);segments.Add(new GlyphSegment(GlyphSegmentKind.Quad,end.X,end.Y,p.X,p.Y));}
            }
            segments.Add(new GlyphSegment(GlyphSegmentKind.Close));
        }
        return segments;
    }
}
