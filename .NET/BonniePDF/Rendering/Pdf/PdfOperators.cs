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
    /// <c>q</c> — Save the current graphics state on the stack.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void SaveState(StringBuilder s) => s.AppendLine("q");

    /// <summary>
    /// <c>Q</c> — Restore the graphics state from the stack.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void RestoreState(StringBuilder s) => s.AppendLine("Q");

    /// <summary>
    /// <c>cm</c> — Concatenate a translation matrix, moving the origin to (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void Translate(StringBuilder s, float x, float y)
        => s.AppendLine($"1 0 0 1 {PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} cm");

    /// <summary>
    /// <c>cm</c> — Concatenate a scaling matrix.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void Scale(StringBuilder s, float sx, float sy)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(sx)} 0 0 {PdfSyntax.FormatDecimal(sy)} 0 0 cm");

    /// <summary>
    /// <c>cm</c> — Concatenate a full 6-element transformation matrix <c>[a b c d e f]</c>.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=211"/>
    /// </summary>
    internal static void ConcatMatrix(StringBuilder s, float a, float b, float c, float d, float e, float f)
        => s.AppendLine(
            $"{PdfSyntax.FormatDecimal(a)} {PdfSyntax.FormatDecimal(b)} {PdfSyntax.FormatDecimal(c)} {PdfSyntax.FormatDecimal(d)} {PdfSyntax.FormatDecimal(e)} {PdfSyntax.FormatDecimal(f)} cm");

    /// <summary>
    /// <c>gs</c> — Set the named graphics state dictionary.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=219"/>
    /// </summary>
    internal static void SetGraphicsState(StringBuilder s, string name)
        => s.AppendLine($"{name} gs");

    // ── Color (PDF Ref 8.6.8) ───────────────────────────────────────────

    /// <summary>
    /// <c>rg</c> — Set the fill color in DeviceRGB.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=287"/>
    /// </summary>
    internal static void SetFillColor(StringBuilder s, Color color)
        => s.AppendLine($"{PdfSyntax.FormatColor(color)} rg");

    /// <summary>
    /// <c>RG</c> — Set the stroke color in DeviceRGB.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=287"/>
    /// </summary>
    internal static void SetStrokeColor(StringBuilder s, Color color)
        => s.AppendLine($"{PdfSyntax.FormatColor(color)} RG");

    // ── Line/Path Graphics (PDF Ref 8.5.3) ──────────────────────────────

    /// <summary>
    /// <c>w</c> — Set the line width.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=216"/>
    /// </summary>
    internal static void SetLineWidth(StringBuilder s, float width)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(width)} w");

    /// <summary>
    /// <c>j</c> — Set the line join style (0=miter, 1=round, 2=bevel).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=216"/>
    /// </summary>
    internal static void SetLineJoin(StringBuilder s, int style)
        => s.AppendLine($"{style} j");

    /// <summary>
    /// <c>M</c> — Set the miter limit.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=217"/>
    /// </summary>
    internal static void SetMiterLimit(StringBuilder s, float limit)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(limit)} M");

    /// <summary>
    /// <c>m</c> — Begin a new subpath at point (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=226"/>
    /// </summary>
    internal static void MoveTo(StringBuilder s, float x, float y)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} m");

    /// <summary>
    /// <c>l</c> — Append a straight line to (<paramref name="x"/>, <paramref name="y"/>).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=226"/>
    /// </summary>
    internal static void LineTo(StringBuilder s, float x, float y)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} l");

    /// <summary>
    /// <c>c</c> — Append a cubic Bézier curve.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void CurveTo(StringBuilder s, float x1, float y1, float x2, float y2, float x3, float y3)
        => s.AppendLine(
            $"{PdfSyntax.FormatDecimal(x1)} {PdfSyntax.FormatDecimal(y1)} {PdfSyntax.FormatDecimal(x2)} {PdfSyntax.FormatDecimal(y2)} {PdfSyntax.FormatDecimal(x3)} {PdfSyntax.FormatDecimal(y3)} c");

    /// <summary>
    /// <c>re</c> — Append a rectangle to the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void Rectangle(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLine(
            $"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re");

    /// <summary>
    /// <c>h</c> — Close the current subpath.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=227"/>
    /// </summary>
    internal static void ClosePath(StringBuilder s) => s.AppendLine("h");

    // ── Path Painting (PDF Ref 8.5.3) ───────────────────────────────────

    /// <summary>
    /// <c>S</c> — Stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void Stroke(StringBuilder s) => s.AppendLine("S");

    /// <summary>
    /// Convenience: <c>re f</c> — Append a rectangle and fill it.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=227"/>
    /// </summary>
    internal static void RectangleFill(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re f");

    /// <summary>
    /// Convenience: <c>re S</c> — Append a rectangle and stroke it.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=227"/>
    /// </summary>
    internal static void RectangleStroke(StringBuilder s, float x, float y, float width, float height)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} {PdfSyntax.FormatDecimal(width)} {PdfSyntax.FormatDecimal(height)} re S");

    // ── Text (PDF Ref 9.3 / 9.4) ───────────────────────────────────────

    /// <summary>
    /// <c>Tw</c> — Set the word spacing: extra space added after each ASCII space character.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=398"/>
    /// </summary>
    internal static void SetWordSpacing(StringBuilder s, float spacing)
        => s.AppendLine($"{PdfSyntax.FormatDecimal(spacing)} Tw");

    /// <summary>
    /// Convenience: <c>m … l S</c> — Draw and stroke a line between two points.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7.pdf#page=226"/>
    /// </summary>
    internal static void StrokeLine(StringBuilder s, float x1, float y1, float x2, float y2)
        => s.AppendLine(
            $"{PdfSyntax.FormatDecimal(x1)} {PdfSyntax.FormatDecimal(y1)} m {PdfSyntax.FormatDecimal(x2)} {PdfSyntax.FormatDecimal(y2)} l S");

    /// <summary>
    /// <c>f</c> — Fill the path using non-zero winding rule.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void Fill(StringBuilder s) => s.AppendLine("f");

    /// <summary>
    /// <c>b</c> — Close, fill, and stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void ClosePathFillStroke(StringBuilder s) => s.AppendLine("b");

    /// <summary>
    /// <c>s</c> — Close and stroke the path.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=230"/>
    /// </summary>
    internal static void ClosePathStroke(StringBuilder s) => s.AppendLine("s");

    // ── Text (PDF Ref 9.3 / 9.4) ───────────────────────────────────────

    /// <summary>
    /// <c>BT</c> — Begin a text object.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=405"/>
    /// </summary>
    internal static void BeginText(StringBuilder s) => s.AppendLine("BT");

    /// <summary>
    /// <c>ET</c> — End a text object.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=405"/>
    /// </summary>
    internal static void EndText(StringBuilder s) => s.AppendLine("ET");

    /// <summary>
    /// <c>Tf</c> — Set the text font and size.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=398"/>
    /// </summary>
    internal static void SetFont(StringBuilder s, string fontResourceName, float size)
        => s.AppendLine($"{fontResourceName} {size} Tf");

    /// <summary>
    /// <c>Tm</c> — Set the text matrix.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=406"/>
    /// </summary>
    internal static void SetTextPosition(StringBuilder s, float x, float y)
        => s.AppendLine($"1 0 0 1 {PdfSyntax.FormatDecimal(x)} {PdfSyntax.FormatDecimal(y)} Tm");

    /// <summary>
    /// <c>Tj</c> — Show a text string.
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=407"/>
    /// </summary>
    internal static void ShowText(StringBuilder s, string operand)
        => s.AppendLine($"{operand} Tj");

    // ── XObject (PDF Ref 8.8) ───────────────────────────────────────────

    /// <summary>
    /// <c>Do</c> — Paint the named XObject (image or form).
    /// <see href="https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.7old.pdf#page=332"/>
    /// </summary>
    internal static void PaintXObject(StringBuilder s, string name)
        => s.AppendLine($"{name} Do");
}