using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BonniePDF.Typography.TrueType;
namespace BonniePDF;

// Whole-program repackaging: glyph-index CIDs, original charstrings, hints and subroutines.
internal static class CffProgramWriter
{
    private static byte[] Join(params byte[][] parts)
    {
        var result = new byte[parts.Sum(p => p.Length)]; int at = 0; foreach (var part in parts) { Buffer.BlockCopy(part, 0, result, at, part.Length); at += part.Length; }
        return result;
    }
    private static byte[] Integer(int n) => new byte[] { 29, (byte)(n >> 24), (byte)(n >> 16), (byte)(n >> 8), (byte)n };
    private static byte[] Real(double n)
    {
        string text = n.ToString("R", CultureInfo.InvariantCulture).ToUpperInvariant(); var digits = new List<int>();
        for (int i = 0; i < text.Length; i++) { char ch = text[i]; if (ch >= '0' && ch <= '9') digits.Add(ch - '0'); else if (ch == '.') digits.Add(10); else if (ch == 'E') { if (text[i + 1] == '-') { digits.Add(12); i++; } else { digits.Add(11); if (text[i + 1] == '+') i++; } } else if (ch == '-') digits.Add(14); else throw new InvalidOperationException("Invalid CFF real."); }
        digits.Add(15); if (digits.Count % 2 != 0) digits.Add(15); var bytes = new byte[1 + digits.Count / 2]; bytes[0] = 30; for (int i = 0; i < digits.Count; i += 2) bytes[1 + i / 2] = (byte)((digits[i] << 4) | digits[i + 1]); return bytes;
    }
    private static byte[] Op(int op, params int[] values) => Join(Join(values.Select(Integer).ToArray()), op >= 1200 ? new byte[] { 12, (byte)(op - 1200) } : new byte[] { (byte)op });
    private static byte[] ArrayOp(int op, double[] values) => Join(Join(values.Select(Real).ToArray()), op >= 1200 ? new byte[] { 12, (byte)(op - 1200) } : new byte[] { (byte)op });
    private static byte[] Index(byte[][] items)
    {
        if (items.Length == 0) return new byte[2]; if (items.Length > 65535) throw new InvalidOperationException("CFF INDEX count exceeded.");
        var header = new byte[3 + (items.Length + 1) * 4]; header[0] = (byte)(items.Length >> 8); header[1] = (byte)items.Length; header[2] = 4; int at = 1;
        for (int i = 0; i <= items.Length; i++) { int p = 3 + i * 4; header[p] = (byte)(at >> 24); header[p + 1] = (byte)(at >> 16); header[p + 2] = (byte)(at >> 8); header[p + 3] = (byte)at; if (i < items.Length) at = checked(at + items[i].Length); }
        return Join(new[] { header }.Concat(items).ToArray());
    }
    internal static byte[] Build(TtfFont font, string name)
    {
        var data = font.ReadEngine(face => (face.Font.Cff ?? throw new InvalidOperationException("Expected CFF outlines.")).GetEmbeddingData());
        byte[] names = Index(new[] { Encoding.ASCII.GetBytes(name) }), strings = Index(new[] { Encoding.ASCII.GetBytes("Adobe"), Encoding.ASCII.GetBytes("Identity") }), globals = Index(data.Globals), chars = Index(data.Chars);
        var charset = new byte[1 + (data.Chars.Length - 1) * 2]; for (int i = 1; i < data.Chars.Length; i++) { charset[i * 2 - 1] = (byte)(i >> 8); charset[i * 2] = (byte)i; }
        var select = new byte[data.Selection.Length + 1]; for (int i = 0; i < data.Selection.Length; i++) select[i + 1] = (byte)data.Selection[i];
        byte[] Fd(int[] offsets) => Index(data.Privates.Select((_, i) => Join(Op(18, data.PrivateSizes[i], offsets[i]), ArrayOp(1207, data.FdMatrices[i]))).ToArray());
        byte[] Top(int charsetAt, int selectAt, int fdAt, int charsAt) => Join(Op(1230, 391, 392, 0), Op(1234, data.Chars.Length), ArrayOp(5, data.BBox), ArrayOp(1207, data.Matrix), Op(15, charsetAt), Op(1237, selectAt), Op(1236, fdAt), Op(17, charsAt));
        var head = new byte[] { 1, 0, 4, 4 }; int charsetOffset = head.Length + names.Length + Index(new[] { Top(0, 0, 0, 0) }).Length + strings.Length + globals.Length;
        int selectOffset = charsetOffset + charset.Length, fdOffset = selectOffset + select.Length, charsOffset = fdOffset + Fd(new int[data.Privates.Length]).Length;
        int at = charsOffset + chars.Length; var offsets = new int[data.Privates.Length]; for (int i = 0; i < offsets.Length; i++) { offsets[i] = at; at += data.Privates[i].Length; }
        return Join(new[] { head, names, Index(new[] { Top(charsetOffset, selectOffset, fdOffset, charsOffset) }), strings, globals, charset, select, Fd(offsets), chars }.Concat(data.Privates).ToArray());
    }
    internal static string EncodingCMap(IReadOnlyDictionary<ushort, ushort> codes)
    {
        var s = new StringBuilder("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >> def\n/CMapName /BonnieCffIdentity def\n/CMapType 1 def\n/WMode 0 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
        var entries = codes.OrderBy(e => e.Key).ToArray(); for (int i = 0; i < entries.Length; i += 100) { int count = Math.Min(100, entries.Length - i); s.Append(count.ToString(CultureInfo.InvariantCulture)).Append(" begincidchar\n"); for (int j = 0; j < count; j++) { var e = entries[i + j]; s.Append('<').Append(e.Key.ToString("X4", CultureInfo.InvariantCulture)).Append("> ").Append(e.Value.ToString(CultureInfo.InvariantCulture)).Append('\n'); } s.Append("endcidchar\n"); }
        return s.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend").ToString();
    }
}