using System.Collections.Generic;
using System.Linq;
using System.Text;
using BonniePDF.Typography.Engine;
using BonniePDF.Typography.TrueType;
namespace BonniePDF;
internal static class EmbeddedTextShaping
{
    internal static GlyphRun Shape(TtfFont font, FontFace face, string text, IReadOnlyDictionary<string,int>? features)
    {
        ShapeOptions? options = features is null || features.Count == 0 ? null : new ShapeOptions(features:features.Select(f=>new ShapeFeature(f.Key,f.Value)));
        // The bidirectional and mixed-script paths are separate methods: their closures capture
        // parameters, which would otherwise allocate the closure object on every call.
        if (text.Length > 0 && (TextFlow.RightToLeft || TextFlow.NeedsBidi(text))) return ShapeBidi(face, text, options);
        if (font.HasColour)
        {
            try { return Shaper.Shape(face, text, options).Run; }
            catch (System.NotSupportedException) { /* Preserve direct mapping for unsupported scripts. */ }
        }
        bool ascii = text.Length > 0;
        for (int i = 0; ascii && i < text.Length; i++) ascii = text[i] >= 32 && text[i] <= 126;
        if (ascii)
        {
            try { return Shaper.Shape(face, text, options).Run; }
            catch (System.NotSupportedException) { /* Preserve the direct-mapping fallback below. */ }
        }
        return ShapeByScript(font, face, text, options);
    }

    private static GlyphRun ShapeBidi(FontFace face, string text, ShapeOptions? options)
    {
        var line = TextFlow.CurrentLine;
        if (line is not null && line.Paragraph.Substring(line.Start, line.End - line.Start) != text) line = null;
        string paragraph = line?.Paragraph ?? text;
        int lineStart = line?.Start ?? 0, lineEnd = line?.End ?? text.Length;
        var resolved = TextItemizer.Resolve(paragraph, (_, _, _, _) => face,
            TextFlow.RightToLeft ? "rtl" : "ltr");
        var shapedLine = new GlyphRun.Builder();
        foreach (var item in resolved.ShapeLine(lineStart, lineEnd, options))
            foreach (var g in item.Glyphs)
                shapedLine.Add(g.Id, g.Cluster - lineStart, g.AdvanceX, g.AdvanceY, g.OffsetX, g.OffsetY,
                    g.ComponentCount, g.IsMark, g.IsLigature, g.SourceLength, g.UnsafeToBreak, g.ComponentIndex);
        return shapedLine.Build();
    }

    // Latin spans through the shaper, everything else mapped directly.
    private static GlyphRun ShapeByScript(TtfFont font, FontFace face, string text, ShapeOptions? options)
    {
        var glyphs=new GlyphRun.Builder(text.Length); int start=0, cluster=0; var span=new StringBuilder();
        void Direct(int id,int offset,int length) => glyphs.Add(id,offset,font.GetGlyphWidth((ushort)id),0,0,0,1,false,false,length,false,0);
        void Flush()
        {
            if(span.Length==0) return;
            string value=span.ToString();
            try {
                var shaped=Shaper.Shape(face,value,options).Run;
                for(int i=0;i<shaped.Count;i++) glyphs.Add(shaped,i,start);
            } catch(System.NotSupportedException) {
                int offset=start;
                foreach(var c in value.EnumerateRunes()) { int id=font.GetGlyphId(c.Value); if(id!=0) Direct(id,offset,c.Utf16SequenceLength); offset+=c.Utf16SequenceLength; }
            }
            span.Clear();
        }
        foreach(var c in text.EnumerateRunes()) {
            int cp=c.Value; bool printable=cp>=32 && !(cp>=127&&cp<=159);
            if(printable && IsLatin(cp)) { if(span.Length==0) start=cluster; span.Append(c.ToString()); }
            else { Flush(); if(printable) {int id=font.GetGlyphId(cp); if(id!=0) Direct(id,cluster,c.Utf16SequenceLength);} }
            cluster+=c.Utf16SequenceLength;
        }
        Flush(); return glyphs.Build();
    }
    private static bool IsLatin(int cp) => cp<=0x36f || cp>=0x1d00&&cp<=0x1eff || cp>=0x2000&&cp<=0x200b || cp>=0x2010&&cp<=0x2029 || cp>=0x202f&&cp<=0x2065 || cp>=0x20a0&&cp<=0x20cf || cp>=0x2100&&cp<=0x214f || cp>=0x2c60&&cp<=0x2c7f || cp>=0xa720&&cp<=0xa7ff || cp>=0xab30&&cp<=0xab6f || cp>=0xfb00&&cp<=0xfb06 || cp==0xfeff;
}
