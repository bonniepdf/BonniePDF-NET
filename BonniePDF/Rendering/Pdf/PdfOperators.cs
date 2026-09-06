using System;
using System.Linq;
using System.Text;
using BonniePDF.Core;

namespace BonniePDF.Rendering.Pdf;

/// <summary>
/// Thin wrappers around PDF content stream operators (PDF Reference 1.7).
/// Each method appends one operator to the stream, replacing raw string literals
/// with self-documenting, compile-time-safe calls.
/// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf"/>
/// </summary>
internal static class PdfOperators
{
    // ── Graphics State (PDF Ref 8.4.4) ──────────────────────────────────

    /// <summary>
    /// <c>q</c> - Save the current graphics state on the stack.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void SaveState(StringBuilder s) => s.AppendLf("q");

    /// <summary>
    /// <c>Q</c> - Restore the graphics state from the stack.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void RestoreState(StringBuilder s) => s.AppendLf("Q");

    /// <summary>
    /// <c>cm</c> - Concatenate a translation matrix, moving the origin to (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void Translate(StringBuilder s, float x, float y)
        => s.AppendLf($"1 0 0 1 {PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} cm");

    /// <summary>
    /// <c>cm</c> - Concatenate a scaling matrix.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void Scale(StringBuilder s, float sx, float sy)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(sx)} 0 0 {PdfSyntax.FormatDecimal(sy)} 0 0 cm");

    /// <summary>
    /// <c>cm</c> - Concatenate a rotation matrix. <paramref name="angleDegrees"/> is in degrees (counter-clockwise).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void Rotate(StringBuilder s, float angleDegrees)
    {
        float radians = angleDegrees * (float)(Math.PI / 180.0);
        float cos = (float)Math.Cos(radians);
        float sin = (float)Math.Sin(radians);
        s.AppendLf($"{PdfSyntax.FormatDecimal(cos)} {PdfSyntax.FormatDecimal(sin)} {PdfSyntax.FormatDecimal(-sin)} {PdfSyntax.FormatDecimal(cos)} 0 0 cm");
    }

    /// <summary>
    /// <c>cm</c> - Concatenate a full 6-element transformation matrix <c>[a b c d e f]</c>.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void ConcatMatrix(StringBuilder s, float a, float b, float c, float d, float e, float f)
        => s.AppendLf(
            $"{PdfSyntax.FormatDecimal(a)} {PdfSyntax.FormatDecimal(b)} {PdfSyntax.FormatDecimal(c)} {PdfSyntax.FormatDecimal(d)} {PdfSyntax.FormatDecimal(e)} {PdfSyntax.FormatDecimal(f)} cm");

    /// <summary>
    /// <c>gs</c> - Set the named graphics state dictionary.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void SetGraphicsState(StringBuilder s, string name)
        => s.AppendLf($"{name} gs");

    /// <summary>
    /// <c>sh</c> - Paint the named shading across the current clip region.
    /// </summary>
    internal static void PaintShading(StringBuilder s, string name)
        => s.AppendLf($"{name} sh");

    // ── Color (PDF Ref 8.6.8) ───────────────────────────────────────────

    /// <summary>
    /// <c>rg</c> - Set the fill color in DeviceRGB.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=287"/>
    /// </summary>
    internal static void SetFillColor(StringBuilder s, Color color)
        => s.AppendLf($"{PdfSyntax.FormatColor(color)} rg");

    /// <summary>
    /// <c>RG</c> - Set the stroke color in DeviceRGB.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=287"/>
    /// </summary>
    internal static void SetStrokeColor(StringBuilder s, Color color)
        => s.AppendLf($"{PdfSyntax.FormatColor(color)} RG");

    // ── Line/Path Graphics (PDF Ref 8.5.3) ──────────────────────────────

    /// <summary>
    /// <c>w</c> - Set the line width.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=216"/>
    /// </summary>
    internal static void SetLineWidth(StringBuilder s, float width)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(width)} w");

    /// <summary>
    /// <c>j</c> - Set the line join style (0=miter, 1=round, 2=bevel).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=216"/>
    /// </summary>
    internal static void SetLineJoin(StringBuilder s, int style)
        => s.AppendLf($"{style} j");

    /// <summary>
    /// <c>M</c> - Set the miter limit.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=217"/>
    /// </summary>
    internal static void SetMiterLimit(StringBuilder s, float limit)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(limit)} M");

    /// <summary>
    /// <c>m</c> - Begin a new subpath at point (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=226"/>
    /// </summary>
    internal static void MoveTo(StringBuilder s, float x, float y)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} m");

    /// <summary>
    /// <c>l</c> - Append a straight line to (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=226"/>
    /// </summary>
    internal static void LineTo(StringBuilder s, float x, float y)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} l");

    /// <summary>
    /// <c>c</c> - Append a cubic Bézier curve.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void CurveTo(StringBuilder s, float x1, float y1, float x2, float y2, float x3, float y3)
        => s.AppendLf(
            $"{PdfSyntax.FormatDecimal(x1)} {PdfSyntax.FormatDecimal(y1)} {PdfSyntax.FormatDecimal(x2)} {PdfSyntax.FormatDecimal(y2)} {PdfSyntax.FormatDecimal(x3)} {PdfSyntax.FormatDecimal(y3)} c");

    /// <summary>
    /// <c>re</c> - Append a rectangle to the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void Rectangle(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLf(
            $"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re");

    /// <summary>
    /// <c>h</c> - Close the current subpath.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void ClosePath(StringBuilder s) => s.AppendLf("h");

    /// <summary>
    /// <c>W n</c> - intersect the current clipping path with the current path
    /// (non-zero winding rule), then end the path without painting it. All
    /// subsequent marking operators are confined to the intersection until the
    /// enclosing <c>q</c>/<c>Q</c> restores the previous clip.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=235"/>
    /// </summary>
    internal static void Clip(StringBuilder s) => s.AppendLf("W n");

    // ── Path Painting (PDF Ref 8.5.3) ───────────────────────────────────

    /// <summary>
    /// <c>S</c> - Stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void Stroke(StringBuilder s) => s.AppendLf("S");

    /// <summary>
    /// Convenience: <c>re f</c> - Append a rectangle and fill it.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=227"/>
    /// </summary>
    internal static void RectangleFill(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re f");

    /// <summary>
    /// Convenience: <c>re S</c> - Append a rectangle and stroke it.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=227"/>
    /// </summary>
    internal static void RectangleStroke(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re S");

    // ── Text (PDF Ref 9.3 / 9.4) ───────────────────────────────────────

    /// <summary>
    /// <c>Tw</c> - Set the word spacing: extra space added after each ASCII space character.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=398"/>
    /// </summary>
    internal static void SetWordSpacing(StringBuilder s, float spacing)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(spacing)} Tw");

    /// <summary>
    /// Emits the character-spacing operator (<c>Tc</c>). Applies to every glyph
    /// shown by both <c>Tj</c> and <c>TJ</c>, for simple and composite fonts
    /// alike, so it is the universal mechanism for letter spacing.
    /// </summary>
    internal static void SetCharacterSpacing(StringBuilder s, float spacing)
        => s.AppendLf($"{PdfSyntax.FormatDecimal(spacing)} Tc");

    /// <summary>
    /// Convenience: <c>m … l S</c> - Draw and stroke a line between two points.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=226"/>
    /// </summary>
    internal static void StrokeLine(StringBuilder s, float x1, float y1, float x2, float y2)
        => s.AppendLf(
            $"{PdfSyntax.FormatDecimal(x1)} {PdfSyntax.FormatDecimal(y1)} m {PdfSyntax.FormatDecimal(x2)} {PdfSyntax.FormatDecimal(y2)} l S");

    /// <summary>
    /// <c>f</c> - Fill the path using non-zero winding rule.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void Fill(StringBuilder s) => s.AppendLf("f");

    /// <summary>
    /// <c>b</c> - Close, fill, and stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void ClosePathFillStroke(StringBuilder s) => s.AppendLf("b");

    /// <summary>
    /// <c>s</c> - Close and stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void ClosePathStroke(StringBuilder s) => s.AppendLf("s");

    // ── Text (PDF Ref 9.3 / 9.4) ───────────────────────────────────────

    /// <summary>
    /// <c>BT</c> - Begin a text object.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=405"/>
    /// </summary>
    internal static void BeginText(StringBuilder s) => s.AppendLf("BT");

    /// <summary>
    /// <c>ET</c> - End a text object.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=405"/>
    /// </summary>
    internal static void EndText(StringBuilder s) => s.AppendLf("ET");

    /// <summary>
    /// <c>Tf</c> - Set the text font and size.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=398"/>
    /// </summary>
    internal static void SetFont(StringBuilder s, string fontResourceName, float size)
        => s.AppendLf($"{fontResourceName} {size} Tf");

    /// <summary>
    /// <c>Tm</c> - Set the text matrix.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=406"/>
    /// </summary>
    internal static void SetTextPosition(StringBuilder s, float x, float y)
        => s.AppendLf($"1 0 0 1 {PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} Tm");

    /// <summary>
    /// <c>Tj</c> - Show a text string.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=407"/>
    /// </summary>
    internal static void ShowText(StringBuilder s, string operand)
        => s.AppendLf($"{operand} Tj");

    /// <summary>
    /// <c>TJ</c> - Show text with individual glyph positioning. The operand is a
    /// pre-built array, e.g. <c>[&lt;0024&gt; 80 &lt;0057&gt;]</c>, where numbers are
    /// advance adjustments in thousandths of an em, <em>subtracted</em> from the
    /// current position (so a tightening kern of -80 font units is emitted as <c>80</c>).
    /// </summary>
    internal static void ShowTextPositioned(StringBuilder s, string arrayOperand)
        => s.AppendLf($"{arrayOperand} TJ");

    // ── XObject (PDF Ref 8.8) ───────────────────────────────────────────

    /// <summary>
    /// <c>Do</c> - Paint the named XObject (image or form).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=332"/>
    /// </summary>
    internal static void PaintXObject(StringBuilder s, string name)
        => s.AppendLf($"{name} Do");

    // ── Marked content (PDF Ref 14.6 - tagged PDF / PDF/UA) ──────────────

    /// <summary>
    /// <c>/&lt;role&gt; &lt;&lt; /MCID n &gt;&gt; BDC</c> - begin a
    /// marked-content sequence with a PDF structure role and an MCID.
    /// The content emitted up to the matching <see cref="EndMarkedContent"/>
    /// is associated with the structure element holding this MCID.
    /// </summary>
    internal static void BeginMarkedContentTagged(StringBuilder s, string role, int mcid)
        => s.AppendLf($"/{role} << /MCID {mcid} >> BDC");

    /// <summary>
    /// <c>EMC</c> - end the most recent marked-content sequence.
    /// </summary>
    internal static void EndMarkedContent(StringBuilder s) => s.AppendLf("EMC");

    /// <summary>
    /// <c>/&lt;role&gt; BMC</c> - begin a marked-content sequence with a
    /// PDF structure role but <em>without</em> an MCID. Use for container
    /// roles (Table, TR, THead, TBody, TFoot, Sect, …) whose content is
    /// referenced by child structure elements rather than directly by an
    /// MCID. Prevents "Nested MCID" warnings in veraPDF when an outer BDC
    /// is already open.
    /// </summary>
    internal static void BeginMarkedContentBmc(StringBuilder s, string role)
        => s.AppendLf($"/{role} BMC");

    /// <summary>Emit <c>/Artifact BMC</c> (no MCID, not in struct tree).</summary>
    internal static void BeginArtifact(StringBuilder s) => s.AppendLf("/Artifact BMC");

    // ── Extended path / state operators (Vector Drawing Surface) ───

    /// <summary>
    /// <c>J</c> - Set the line cap style (0=butt, 1=round, 2=square).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=216"/>
    /// </summary>
    internal static void SetLineCap(StringBuilder s, int style)
        => s.AppendLf($"{style} J");

    /// <summary>
    /// <c>d</c> - Set the dash pattern and phase.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=217"/>
    /// </summary>
    internal static void SetDashPattern(StringBuilder s, float[] pattern, float phase)
    {
        if (pattern == null || pattern.Length == 0)
        {
            s.AppendLf("[] 0 d");
            return;
        }
        var parts = string.Join(" ", pattern.Select(p => PdfSyntax.FormatDecimal(p)));
        s.AppendLf($"[{parts}] {PdfSyntax.FormatDecimal(phase)} d");
    }

    /// <summary>
    /// <c>W* n</c> - Intersect the current clipping path with the current path
    /// using the even-odd rule, then end the path without painting it.
    /// </summary>
    internal static void ClipEvenOdd(StringBuilder s) => s.AppendLf("W* n");

    /// <summary>
    /// <c>f*</c> - Fill the path using the even-odd rule.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void FillEvenOdd(StringBuilder s) => s.AppendLf("f*");
}
