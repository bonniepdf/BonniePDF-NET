using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BonniePDF.Compliance;
using BonniePDF.Rendering;

namespace BonniePDF.Export;

/// <summary>
/// Writes an <see cref="SvgDocumentExport"/> as JSON under the shared JSON
/// conventions both implementations follow: canonical camelCase keys, unset
/// values omitted rather than written as <c>null</c>, enumerations as their
/// lower-case wire strings, and numbers in their shortest round-trip form.
/// </summary>
/// <remarks>
/// The contract is written out member by member rather than left to reflection:
/// default <c>System.Text.Json</c> serialization of these types would produce
/// PascalCase keys, <c>null</c> members and an integer severity, none of which
/// the TypeScript implementation's <c>JSON.stringify</c> output contains.
/// </remarks>
internal static class SvgDocumentExportJson
{
    private static readonly JsonWriterOptions Options = new()
    {
        // JSON.stringify leaves <, > and & as they are; so does this, which keeps
        // SVG markup from growing six characters per tag.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
    };

    public static string Write(SvgDocumentExport export)
    {
        using var buffer = new MemoryStream();
        using (var w = new Utf8JsonWriter(buffer, Options))
        {
            w.WriteStartObject();
            w.WriteNumber("version", export.Version);
            w.WriteStartArray("pages");
            foreach (string page in export.Pages) w.WriteStringValue(page);
            w.WriteEndArray();
            w.WriteString("fontFaceCss", export.FontFaceCss);
            if (export.Snapshot is not null)
            {
                w.WritePropertyName("snapshot");
                WriteSnapshot(w, export.Snapshot);
            }
            if (export.Report is not null)
            {
                w.WritePropertyName("report");
                WriteReport(w, export.Report);
            }
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteSnapshot(Utf8JsonWriter w, LayoutSnapshot snapshot)
    {
        w.WriteStartObject();
        w.WriteNumber("version", snapshot.Version);
        w.WriteStartArray("pages");
        foreach (LayoutSnapshot.PageSize page in snapshot.Pages)
        {
            w.WriteStartObject();
            w.WriteNumber("width", page.Width);
            w.WriteNumber("height", page.Height);
            if (page.Color is not null) w.WriteString("color", page.Color);
            if (page.BackdropUnknown) w.WriteBoolean("backdropUnknown", true);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WritePropertyName("root");
        WriteNode(w, snapshot.Root);
        if (snapshot.Meta is not null)
        {
            SnapshotMeta meta = snapshot.Meta;
            w.WriteStartObject("meta");
            // Both keep the spelling the snapshot has always used (an integer and
            // the enum name) until the snapshot's own version next changes.
            w.WriteNumber("accessibilityMode", meta.AccessibilityMode);
            w.WriteString("conformance", meta.Conformance);
            if (meta.Language is not null) w.WriteString("language", meta.Language);
            if (meta.Title is not null) w.WriteString("title", meta.Title);
            w.WriteEndObject();
        }
        w.WriteEndObject();
    }

    private static void WriteNode(Utf8JsonWriter w, LayoutNode node)
    {
        w.WriteStartObject();
        w.WriteString("id", node.Id);
        w.WriteString("type", node.Type);
        if (node.Name is not null) w.WriteString("name", node.Name);
        if (node.SourceId is not null) w.WriteString("sourceId", node.SourceId);
        w.WriteStartArray("rects");
        foreach (LayoutRect rect in node.Rects)
        {
            w.WriteStartObject();
            w.WriteNumber("page", rect.Page);
            w.WriteNumber("x", rect.X);
            w.WriteNumber("y", rect.Y);
            w.WriteNumber("width", rect.Width);
            w.WriteNumber("height", rect.Height);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        if (node.A11y is not null)
        {
            NodeAccessibility a11y = node.A11y;
            w.WriteStartObject("a11y");
            w.WriteString("role", a11y.Role);
            w.WriteBoolean("tagged", a11y.Tagged);
            w.WriteBoolean("artifact", a11y.Artifact);
            if (a11y.HeadingLevel is int level) w.WriteNumber("headingLevel", level);
            if (a11y.ImageLike is bool imageLike) w.WriteBoolean("imageLike", imageLike);
            if (a11y.Alt is not null) w.WriteString("alt", a11y.Alt);
            if (a11y.Decorative is bool decorative) w.WriteBoolean("decorative", decorative);
            w.WriteEndObject();
        }
        if (node.Paint is not null)
        {
            NodePaint paint = node.Paint;
            w.WriteStartObject("paint");
            if (paint.Text is not null) w.WriteString("text", paint.Text);
            if (paint.TextAlpha is float textAlpha) w.WriteNumber("textAlpha", textAlpha);
            if (paint.Background is not null) w.WriteString("background", paint.Background);
            if (paint.BackgroundAlpha is float backgroundAlpha) w.WriteNumber("backgroundAlpha", backgroundAlpha);
            if (paint.FontSize is float fontSize) w.WriteNumber("fontSize", fontSize);
            if (paint.Bold is bool bold) w.WriteBoolean("bold", bold);
            if (paint.BackdropUnknown is bool backdropUnknown) w.WriteBoolean("backdropUnknown", backdropUnknown);
            w.WriteEndObject();
        }
        w.WriteStartArray("children");
        foreach (LayoutNode child in node.Children) WriteNode(w, child);
        w.WriteEndArray();
        w.WriteEndObject();
    }

    private static void WriteReport(Utf8JsonWriter w, ComplianceReport report)
    {
        w.WriteStartObject();
        w.WriteStartArray("findings");
        foreach (ComplianceFinding finding in report.Findings)
        {
            w.WriteStartObject();
            w.WriteString("code", finding.Code);
            w.WriteString("severity", SeverityToken(finding.Severity));
            w.WriteString("standard", finding.Standard);
            w.WriteString("clause", finding.Clause);
            w.WriteString("message", finding.Message);
            if (finding.FixHint is not null) w.WriteString("fixHint", finding.FixHint);
            if (finding.NodeId is not null) w.WriteString("nodeId", finding.NodeId);
            if (finding.Role is not null) w.WriteString("role", finding.Role);
            if (finding.Page is int page) w.WriteNumber("page", page);
            w.WriteEndObject();
        }
        w.WriteEndArray();
        w.WriteBoolean("passed", report.Passed);
        w.WriteNumber("errorCount", report.ErrorCount);
        w.WriteNumber("warningCount", report.WarningCount);
        w.WriteNumber("infoCount", report.InfoCount);
        w.WriteEndObject();
    }

    /// <summary>The wire value of a severity. Never the integer behind the enum.</summary>
    private static string SeverityToken(ComplianceSeverity severity) => severity switch
    {
        ComplianceSeverity.Error => "error",
        ComplianceSeverity.Warning => "warning",
        ComplianceSeverity.Info => "info",
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown compliance severity."),
    };
}
