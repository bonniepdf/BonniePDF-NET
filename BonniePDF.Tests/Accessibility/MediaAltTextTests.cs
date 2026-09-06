using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Shapes;
using BonniePDF.Rendering.Tagging;
using Xunit;

namespace BonniePDF.Tests.Accessibility;

/// <summary>
/// Alt-text on media + implicit Figure / Artifact tagging.
/// Mostly unit-tested via QRCode end-to-end (QRCode doesn't need a
/// real image fixture); Image is tested at the property / role level
/// without rendering.
/// </summary>
public class MediaAltTextTests
{
    // ── Image role / alt-text wiring (property-level) ───────────────

    [Fact]
    public void Image_WithAltText_RoleIsFigure()
    {
        var img = new Image { FilePath = "logo.png", AltText = "Company logo" };
        Assert.Equal(SemanticRole.Figure, img.SemanticRole);
    }

    [Fact]
    public void Image_WithoutAltText_RoleIsArtifact()
    {
        var img = new Image { FilePath = "border.png" };
        Assert.Equal(SemanticRole.Artifact, img.SemanticRole);
    }

    [Fact]
    public void Image_DecorativeOverridesAltText()
    {
        // Explicit decorative trumps a stray AltText.
        var img = new Image
        {
            FilePath = "border.png",
            AltText = "leftover from a copy-paste",
            IsDecorative = true,
        };
        Assert.Equal(SemanticRole.Artifact, img.SemanticRole);
    }

    [Fact]
    public void Image_ImplementsIRoleProviderAndIAltTextProvider()
    {
        var img = new Image();
        Assert.IsAssignableFrom<IRoleProvider>(img);
        Assert.IsAssignableFrom<IAltTextProvider>(img);
    }

    // ── QRCode role / alt-text wiring (property-level) ─────────────

    [Fact]
    public void QRCode_WithAltText_RoleIsFigure()
    {
        var qr = new QRCode { Data = "https://example.com", AltText = "QR code to example.com" };
        Assert.Equal(SemanticRole.Figure, qr.SemanticRole);
    }

    [Fact]
    public void QRCode_WithoutAltText_RoleIsArtifact()
    {
        var qr = new QRCode { Data = "https://example.com" };
        Assert.Equal(SemanticRole.Artifact, qr.SemanticRole);
    }

    // ── End-to-end via QRCode (no fixture needed) ──────────────────

    [Fact]
    public void TaggedDocument_QRCodeWithAltText_EmitsFigureWithAlt()
    {
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new QRCode
            {
                Data = "https://example.com",
                Size = 60,
                AltText = "QR code linking to example.com",
            });
        });

        Assert.Contains("/S /Figure", pdf);
        Assert.Contains("/Alt (QR code linking to example.com)", pdf);
    }

    [Fact]
    public void TaggedDocument_QRCodeWithoutAltText_EmitsArtifact()
    {
        string pdf = GenerateTagged(doc =>
        {
            doc.Add(new QRCode { Data = "https://example.com", Size = 60 });
        });

        // A decorative element is wrapped in /Artifact marked content and kept
        // OUT of the structure tree. Emitting /S /Artifact (a StructElem) is
        // invalid - Artifact is a marked-content type, not a structure type
        // (VeraPDF 7.1-5). The /Artifact BMC operator lives in the (compressed)
        // content stream, so we assert the structure-tree negatives here.
        Assert.DoesNotContain("/S /Artifact", pdf);
        Assert.DoesNotContain("/S /Figure", pdf);
        Assert.DoesNotContain("/Alt (", pdf);
    }

    [Fact]
    public void UntaggedDocument_DoesNotEmitFigureOrAlt()
    {
        // Back-compat: accessibility off => no struct elements at all.
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.None;
        doc.Add(new QRCode { Data = "https://example.com", Size = 60, AltText = "ignored when tagging off" });

        using var ms = new MemoryStream();
        doc.Save(ms, true);
        string pdf = Encoding.ASCII.GetString(ms.ToArray());

        Assert.DoesNotContain("/S /Figure", pdf);
        Assert.DoesNotContain("/Alt (", pdf);
    }

    private static string GenerateTagged(System.Action<BonniePDF.Document> populate)
    {
        using var doc = new BonniePDF.Document();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        populate(doc);
        using var ms = new MemoryStream();
        doc.Save(ms, true);
        return Encoding.ASCII.GetString(ms.ToArray());
    }
}
