using System.Collections.Generic;
using System.Globalization;
using System.Text;
using BonniePDF.Typography.Engine;
namespace BonniePDF;
internal static class GlyphOutlinePath
{
    internal static string Build(FontFace face,int glyph)
    {
        var parts=new List<string>();
        var outline=face.Font.ReadGlyph(glyph);
        foreach(var c in outline.Commands)parts.Add(c[0]==0?$"M{c[1].ToString(CultureInfo.InvariantCulture)} {c[2].ToString(CultureInfo.InvariantCulture)}":c[0]==1?$"L{c[1].ToString(CultureInfo.InvariantCulture)} {c[2].ToString(CultureInfo.InvariantCulture)}":c[0]==2?"C"+string.Join(" ",System.Linq.Enumerable.Select(System.Linq.Enumerable.Skip(c,1),n=>n.ToString(CultureInfo.InvariantCulture))):"Z");
        foreach(var contour in outline.Contours)
        {
            if(contour.Count<2)continue;
            var points=new List<FontPoint>();
            for(int i=0;i<contour.Count;i++) {var a=contour[i];var b=contour[(i+1)%contour.Count];points.Add(a);if(!a.OnCurve&&!b.OnCurve)points.Add(new FontPoint((a.X+b.X)/2,(a.Y+b.Y)/2,true));}
            int start=points.FindIndex(p=>p.OnCurve);if(start<0)continue;FontPoint At(int i)=>points[(start+i)%points.Count];
            string N(double n)=>n.ToString(CultureInfo.InvariantCulture);
            parts.Add($"M{N(At(0).X)} {N(At(0).Y)}");
            for(int i=1;i<points.Count;i++) {var p=At(i);if(p.OnCurve)parts.Add($"L{N(p.X)} {N(p.Y)}");else {var end=At(++i);parts.Add($"Q{N(p.X)} {N(p.Y)} {N(end.X)} {N(end.Y)}");}}parts.Add("Z");
        }
        return string.Join(" ",parts);
    }
}
