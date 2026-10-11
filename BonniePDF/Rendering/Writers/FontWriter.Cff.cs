using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Pdf;
namespace BonniePDF.Rendering.Writers;

internal partial class FontWriter
{
    private sealed record CffObject(int Id, Dictionary<string, string> Dictionary, string? Text = null, byte[]? Bytes = null);
    private static List<CffObject> CffObjects(int type0Id, EmbeddedFontInfo info, Func<int> next)
    {
        var font = info.Font; var tracker = info.Tracker; string name = font.PostScriptName;
        if ((name.Length < 1 || name.Length > 127 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '+' or '-'))) throw new InvalidOperationException("Invalid CFF PostScript font name.");
        int cid = next(), descriptor = next(), program = next(), unicode = next(), encoding = next();
        string Ref(int id) => id.ToString(CultureInfo.InvariantCulture) + " 0 R";
        const string system = "<< /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>";
        double scale = 1000.0 / font.UnitsPerEm;
        string widths = "[ " + string.Join(" ", tracker.CidToGlyphMap.Values.Distinct().Order().Select(g => $"{g.ToString(CultureInfo.InvariantCulture)} [ {PdfSyntax.FormatDecimal((float)(font.GetGlyphWidth(g) * scale))} ]")) + " ]";
        var cmap = new BonniePDF.Typography.TrueType.ToUnicodeCMapBuilder(); foreach (var e in tracker.GlyphSources) cmap.AddSourceMapping(e.Key, e.Value); cmap.AddMappings(tracker.GetUsedCharToGlyphMap());
        return new()
        {
            new(program,new(){[PdfKeys.Common.Subtype]="/CIDFontType0C"},Bytes:CffProgramWriter.Build(font,name)),
            new(descriptor,new()
            {
                [PdfKeys.Common.Type]="/FontDescriptor",[PdfKeys.FontDescriptor.Name]="/"+name,[PdfKeys.FontDescriptor.Flags]=font.Flags.ToString(CultureInfo.InvariantCulture),
                [PdfKeys.FontDescriptor.BoundingBox]="[ "+string.Join(" ",font.BBox.Select(v=>((int)(v*scale)).ToString(CultureInfo.InvariantCulture)))+" ]",
                [PdfKeys.FontDescriptor.ItalicAngle]=font.ItalicAngle.ToString(CultureInfo.InvariantCulture),[PdfKeys.FontDescriptor.Ascent]=((int)(font.Ascender*scale)).ToString(CultureInfo.InvariantCulture),
                [PdfKeys.FontDescriptor.Descent]=((int)(font.Descender*scale)).ToString(CultureInfo.InvariantCulture),[PdfKeys.FontDescriptor.CapHeight]=((int)(font.CapHeight*scale)).ToString(CultureInfo.InvariantCulture),
                [PdfKeys.FontDescriptor.VerticalStemWidth]=font.StemV.ToString(CultureInfo.InvariantCulture),[PdfKeys.FontDescriptor.CffProgram]=Ref(program)
            }),
            new(encoding,new(){[PdfKeys.Common.Type]="/CMap",[PdfKeys.Font.CMapName]="/BonnieCffIdentity",[PdfKeys.Font.CidSystemInfo]=system},CffProgramWriter.EncodingCMap(tracker.CidToGlyphMap)),
            new(cid,new(){[PdfKeys.Common.Type]="/Font",[PdfKeys.Common.Subtype]="/CIDFontType0",[PdfKeys.Font.BaseFont]="/"+name,[PdfKeys.Font.CidSystemInfo]=system,[PdfKeys.Font.Descriptor]=Ref(descriptor),[PdfKeys.Font.Widths]=widths}),
            new(unicode,new(),cmap.Build()),
            new(type0Id,new(){[PdfKeys.Common.Type]="/Font",[PdfKeys.Common.Subtype]="/Type0",[PdfKeys.Font.BaseFont]="/"+name,[PdfKeys.Font.Encoding]=Ref(encoding),[PdfKeys.Font.DescendantFonts]="[ "+Ref(cid)+" ]",[PdfKeys.Font.ToUnicode]=Ref(unicode)})
        };
    }
    private int WriteEmbeddedCff(int id, EmbeddedFontInfo info)
    {
        foreach (var obj in CffObjects(id, info, _writer!.GetNewId))
            if (obj.Bytes is not null) _writer!.WriteCompressedBinaryObject(obj.Id, obj.Dictionary, obj.Bytes);
            else _writer!.WriteObject(obj.Id, obj.Dictionary, obj.Text);
        return id;
    }
    private async Task<int> WriteEmbeddedCffAsync(int id, EmbeddedFontInfo info, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        foreach (var obj in CffObjects(id, info, _asyncWriter!.GetNewId))
            if (obj.Bytes is not null) await _asyncWriter!.WriteCompressedBinaryObjectAsync(obj.Id, obj.Dictionary, obj.Bytes, obj.Bytes.Length, cancellationToken).ConfigureAwait(false);
            else await _asyncWriter!.WriteObjectAsync(obj.Id, obj.Dictionary, obj.Text, cancellationToken).ConfigureAwait(false);
        return id;
    }
}