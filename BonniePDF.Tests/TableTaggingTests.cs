using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Tables;
using BonniePDF.Elements.Text;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

[Collection("Serial")]

/// <summary>
/// table structure tagging. Asserts that a tagged Table
/// produces the <c>/Table > /TR > [/TH | /TD]</c> structure hierarchy
/// per ISO 32000-1 §14.8.4.3, and that <c>ColumnSpan</c> surfaces as
/// the documented <c>/A</c> attributes dict on the cell's StructElem.
/// </summary>
public class TableTaggingTests
{
    private static string GenerateTaggedTable(System.Action<Table> populate, System.Action<Table> hookHeader = null)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";

        Table table = new(120f, 120f, 120f);
        hookHeader?.Invoke(table);
        populate(table);
        doc.Add(table);

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    // ── Role-level (property) checks ────────────────────────────────

    [Fact]
    public void Table_SemanticRoleIsTable()
    {
        var table = new Table(100f);
        Assert.Equal(SemanticRole.Table, table.SemanticRole);
    }

    [Fact]
    public void TableRow_SemanticRoleIsTR()
    {
        var row = new TableRow { Cells = System.Array.Empty<TableCell>() };
        Assert.Equal(SemanticRole.TR, row.SemanticRole);
    }

    [Fact]
    public void TableCell_HeaderRoleIsTH_BodyRoleIsTD()
    {
        var th = new TableCell { Content = new TextBlock { Text = "" }, IsHeader = true };
        var td = new TableCell { Content = new TextBlock { Text = "" } };

        Assert.Equal(SemanticRole.TH, th.SemanticRole);
        Assert.Equal(SemanticRole.TD, td.SemanticRole);
    }

    // ── Byte-level (end-to-end) checks ──────────────────────────────

    [Fact]
    public void TaggedTable_EmitsTableTRAndCellStructElems()
    {
        string pdf = GenerateTaggedTable(
            hookHeader: t => t.AddHeader(
                new TableCell { Content = new TextBlock { Text = "A", FontName = StandardFonts.HelveticaBold } },
                new TableCell { Content = new TextBlock { Text = "B", FontName = StandardFonts.HelveticaBold } },
                new TableCell { Content = new TextBlock { Text = "C", FontName = StandardFonts.HelveticaBold } }),
            populate: t =>
            {
                t.AddRow(new TextBlock { Text = "1" }, new TextBlock { Text = "2" }, new TextBlock { Text = "3" });
                t.AddRow(new TextBlock { Text = "4" }, new TextBlock { Text = "5" }, new TextBlock { Text = "6" });
            });

        Assert.Contains("/S /Table", pdf);
        Assert.Contains("/S /TR", pdf);
        Assert.Contains("/S /TH", pdf);
        Assert.Contains("/S /TD", pdf);
    }

    [Fact]
    public void TaggedTable_HeaderCells_RenderAsTH_BodyCells_RenderAsTD()
    {
        // One header row + one data row, both with 3 cells.
        // Expect: 3 TH StructElems + 3 TD StructElems.
        string pdf = GenerateTaggedTable(
            hookHeader: t => t.AddHeader(
                new TableCell { Content = new TextBlock { Text = "A", FontName = StandardFonts.HelveticaBold } },
                new TableCell { Content = new TextBlock { Text = "B", FontName = StandardFonts.HelveticaBold } },
                new TableCell { Content = new TextBlock { Text = "C", FontName = StandardFonts.HelveticaBold } }),
            populate: t =>
            {
                t.AddRow(new TextBlock { Text = "1" }, new TextBlock { Text = "2" }, new TextBlock { Text = "3" });
            });

        // "/S /TH" also matches "/S /THead", so subtract the THead count.
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /TH") - SubstringCounter.Count(pdf, "/S /THead"));
        Assert.Equal(3, SubstringCounter.Count(pdf, "/S /TD"));
        // Exactly one Table wrapping two rows.
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /Table"));
        Assert.Equal(2, SubstringCounter.Count(pdf, "/S /TR"));
        // PDF/UA 7.2: rows are grouped under THead/TBody.
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /THead"));
        Assert.Equal(1, SubstringCounter.Count(pdf, "/S /TBody"));
    }

    [Fact]
    public void TaggedTable_ColumnSpan_SurfacesAsAttributesColSpan()
    {
        string pdf = GenerateTaggedTable(
            populate: t =>
            {
                t.AddRow(new TableCell
                {
                    Content = new TextBlock { Text = "spans 2" },
                    ColumnSpan = 2,
                },
                new TableCell { Content = new TextBlock { Text = "single" } });
            });

        // The StructElem for the spanning cell carries /A with /ColSpan 2.
        Assert.Contains("/A << /O /Table /ColSpan 2 >>", pdf);
    }

    [Fact]
    public void UntaggedTable_EmitsNoStructElems()
    {
        // Back-compat: with accessibility off, the renderer must not
        // emit any /S /Table, /S /TR, /S /TH, /S /TD at all.
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;

        Table table = new(100f, 100f);
        table.AddRow(new TextBlock { Text = "x" }, new TextBlock { Text = "y" });
        doc.Add(table);

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain("/S /Table", pdf);
        Assert.DoesNotContain("/S /TR", pdf);
        Assert.DoesNotContain("/S /TH", pdf);
        Assert.DoesNotContain("/S /TD", pdf);
    }

    [Fact]
    public void TaggedTable_EveryPageStreamHasBalancedMarkedContent()
    {
        // Even with the nested wrap (Table > TR > TH/TD around cell
        // text), the per-page BDC/EMC counts must stay balanced after
        // the this feature page-break-aware fix.
        // Table and TR are container roles that emit BMC, so count opens = BDC + BMC.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTaggedTable(
                hookHeader: t => t.AddHeader(
                    new TableCell { Content = new TextBlock { Text = "A" } },
                    new TableCell { Content = new TextBlock { Text = "B" } },
                    new TableCell { Content = new TextBlock { Text = "C" } }),
                populate: t =>
                {
                    for (int i = 0; i < 50; i++)
                        t.AddRow(new TextBlock { Text = $"row {i}, a" },
                                 new TextBlock { Text = $"row {i}, b" },
                                 new TextBlock { Text = $"row {i}, c" });
                });

            string[] streams = PdfPageStreamSplitter.Split(pdf);
            Assert.NotEmpty(streams);
            foreach (string stream in streams)
            {
                // Container roles (Table, TR) emit BMC; cell content (TH/TD) emits BDC.
                int opens = SubstringCounter.Count(stream, "BDC") + SubstringCounter.Count(stream, "BMC");
                int emc = SubstringCounter.Count(stream, "EMC");
                Assert.True(opens == emc, $"unbalanced marked-content on a page: opens={opens} EMC={emc}");
            }
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }
}
