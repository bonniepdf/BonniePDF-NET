using System.IO;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Elements.Tables;
using BonniePDF.Rendering.Tagging;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

[Collection("Serial")]

/// <summary>
/// this feature through Stage 2 accessibility hardening.
/// Semantic groups, artifact tagging for headers/footers/watermarks,
/// /Tabs /S reading-order hint, and annotation /StructParent + /OBJR.
/// </summary>
public class Stage2Tests
{
    // SemanticGroup

    [Fact]
    public void SemanticGroup_ImplementsIRoleProvider()
    {
        var g = new SemanticGroup(SemanticRole.Sect);
        Assert.IsAssignableFrom<IRoleProvider>(g);
        Assert.Equal(SemanticRole.Sect, g.SemanticRole);
    }

    [Fact]
    public void SemanticGroup_TaggedDocument_EmitsSectStructElem()
    {
        string pdf = GenerateTagged(doc =>
        {
            var g = new SemanticGroup(SemanticRole.Sect);
            var h = new Heading("Section heading") { TocIdentifier = "s", Level = 1, FontSize = 18 };
            g.AddChild(h);
            var p = new TextBlock { Text = "Body text.", FontSize = 12 };
            g.AddChild(p);
            doc.Add(g);
        });

        Assert.Contains("/S /Sect", pdf);
        Assert.Contains("/S /H1", pdf);
        Assert.Contains("/S /P", pdf);
    }

    [Fact]
    public void VerticalStack_Children_AreTagged()
    {
        string pdf = GenerateTagged(doc =>
        {
            var stack = new VerticalStack();
            stack.AddChild(new Heading("Stack heading") { TocIdentifier = "sh", Level = 2, FontSize = 16 });
            stack.AddChild(new TextBlock { Text = "Stack body.", FontSize = 12 });
            doc.Add(stack);
        });

        Assert.Contains("/S /H2", pdf);
        Assert.Contains("/S /P", pdf);
    }

    [Fact]
    public void NestedLayout_TableInsideVerticalStack_ProducesTableStructure()
    {
        string pdf = GenerateTagged(doc =>
        {
            var stack = new VerticalStack();
            var t = new Table(150f, 150f);
            t.AddHeader(new TextBlock { Text = "A" }, new TextBlock { Text = "B" });
            t.AddRow(new TextBlock { Text = "1" }, new TextBlock { Text = "2" });
            stack.AddChild(t);
            doc.Add(stack);
        });

        Assert.Contains("/S /Table", pdf);
        Assert.Contains("/S /TR", pdf);
        Assert.Contains("/S /TH", pdf);
        Assert.Contains("/S /TD", pdf);
    }

    // Artifact tags

    [Fact]
    public void HeaderTemplate_TaggedDocument_EmitsArtifactBmc()
    {
        // /Artifact BMC is in the page content stream - requires compression off to find it.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(doc =>
            {
                doc.Settings.HeaderTemplate = _ => new TextBlock { Text = "Header", FontSize = 9 };
                doc.Add(new TextBlock { Text = "Body content.", FontSize = 12 });
            });

            Assert.Contains("/Artifact BMC", pdf);
            Assert.Contains("/S /Document", pdf);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void FooterTemplate_TaggedDocument_EmitsArtifactBmc()
    {
        // /Artifact BMC is in the page content stream - requires compression off to find it.
        InternalConfiguration.EnableCompression = false;
        try
        {
            string pdf = GenerateTagged(doc =>
            {
                doc.Settings.FooterTemplate = _ => new TextBlock { Text = "Footer", FontSize = 9 };
                doc.Add(new TextBlock { Text = "Body content.", FontSize = 12 });
            });

            Assert.Contains("/Artifact BMC", pdf);
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    // /Tabs /S

    [Fact]
    public void TaggedDocument_PageDict_ContainsTabsS()
    {
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new TextBlock { Text = "Content.", FontSize = 12 });
        });

        Assert.Contains("/Tabs /S", pdf);
    }

    [Fact]
    public void UntaggedDocument_PageDict_DoesNotContainTabsS()
    {
        string pdf = GenerateUntagged(doc =>
        {
            doc.Add(new TextBlock { Text = "Content.", FontSize = 12 });
        });

        Assert.DoesNotContain("/Tabs /S", pdf);
    }

    // Annotation /StructParent

    [Fact]
    public void TaggedHyperlink_AnnotationCarriesStructParent()
    {
        string pdf = GenerateTagged(doc =>
        {
            var link = new Hyperlink("Visit") { Uri = "https://example.com", FontSize = 12 };
            doc.Add(link);
        });

        Assert.Contains("/StructParent", pdf);
    }

    [Fact]
    public void TaggedHyperlink_StructTreeContainsObjr()
    {
        string pdf = GenerateTagged(doc =>
        {
            var link = new Hyperlink("Visit") { Uri = "https://example.com", FontSize = 12 };
            doc.Add(link);
        });

        Assert.Contains("/OBJR", pdf);
    }

    [Fact]
    public void UntaggedHyperlink_NoStructParent()
    {
        string pdf = GenerateUntagged(doc =>
        {
            var link = new Hyperlink("Visit") { Uri = "https://example.com", FontSize = 12 };
            doc.Add(link);
        });

        Assert.DoesNotContain("/StructParent", pdf);
        Assert.DoesNotContain("/OBJR", pdf);
    }

    // Helpers

    private static string GenerateTagged(System.Action<Document> populate)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }

    private static string GenerateUntagged(System.Action<Document> populate)
    {
        using var doc = new Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
