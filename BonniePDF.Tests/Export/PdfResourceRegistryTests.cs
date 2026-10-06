using Xunit;

namespace BonniePDF.Tests.Export;

// The registry hands out stable, de-duplicated resource names (/F1, /I1, /GS_ALPHA_1, …)
// and avoids emitting the same font/image/graphics-state twice. These tests pin that
// registration + de-duplication logic (writer-dependent serialization is covered elsewhere).
public class PdfResourceRegistryTests
{
    private static ImageInfo ImageWithData(params byte[] bytes) => new() { Data = bytes };

    [Fact]
    public void IsEmbeddedFont_RecognisesTtfOtfAndVirtualKeysCaseInsensitively()
    {
        Assert.True(PdfResourceRegistry.IsEmbeddedFont("font.ttf"));
        Assert.True(PdfResourceRegistry.IsEmbeddedFont("FONT.OTF"));
        Assert.True(PdfResourceRegistry.IsEmbeddedFont("virtual:Helvetica"));
        Assert.False(PdfResourceRegistry.IsEmbeddedFont("Helvetica"));
    }

    [Fact]
    public void RegisterFont_NumbersStandardFontsSequentiallyAndDeduplicates()
    {
        PdfResourceRegistry r = new();
        Assert.Equal("/F1", r.RegisterFont("Helvetica"));
        Assert.Equal("/F1", r.RegisterFont("Helvetica"));   // same identifier => same name
        Assert.Equal("/F2", r.RegisterFont("Times-Roman")); // new identifier => next number
    }

    [Fact]
    public void HasNonEmbeddedStandardFont_ReflectsRegisteredType1Fonts()
    {
        PdfResourceRegistry r = new();
        Assert.False(r.HasNonEmbeddedStandardFont());
        r.RegisterFont("Helvetica");
        Assert.True(r.HasNonEmbeddedStandardFont());
    }

    [Fact]
    public void RegisterImage_NewIdForNewContent_SameIdForIdenticalContent()
    {
        PdfResourceRegistry r = new();
        int id1 = r.RegisterImage(ImageWithData(1, 2, 3));
        int idDup = r.RegisterImage(ImageWithData(1, 2, 3)); // identical bytes
        int id2 = r.RegisterImage(ImageWithData(4, 5, 6));   // different bytes

        Assert.Equal(id1, idDup); // de-duplicated
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void GetNewImageId_IncrementsAndResourceNameMapsToSlashI()
    {
        PdfResourceRegistry r = new();
        Assert.Equal(1, r.GetNewImageId());
        Assert.Equal(2, r.GetNewImageId());
        Assert.Equal("/I2", r.GetImageResourceName(2));
        Assert.Equal("/I2", r.GetImageResourceName(2)); // cached, stable
    }

    [Fact]
    public void RegisterAlphaGraphicsState_NumbersSequentiallyAndDeduplicatesEqualAlphas()
    {
        PdfResourceRegistry r = new();
        Assert.Equal("/GS_ALPHA_1", r.RegisterAlphaGraphicsState(0.5f));
        Assert.Equal("/GS_ALPHA_1", r.RegisterAlphaGraphicsState(0.5f)); // dedup
        Assert.Equal("/GS_ALPHA_2", r.RegisterAlphaGraphicsState(0.25f));
    }

    [Fact]
    public void RegisterAlphaGraphicsState_ClampsIntoUnitRange()
    {
        PdfResourceRegistry r = new();
        string a = r.RegisterAlphaGraphicsState(1.5f); // clamps to 1.0
        string b = r.RegisterAlphaGraphicsState(2.0f); // also clamps to 1.0
        Assert.Equal(a, b);
    }

    [Fact]
    public void RegisterAlphaGraphicsState_ForcesAlphaToOneUnderPdfA1()
    {
        PdfResourceRegistry r = new() { Conformance = PdfAConformance.PdfA1b };
        string a = r.RegisterAlphaGraphicsState(0.5f); // forced to 1.0
        string b = r.RegisterAlphaGraphicsState(0.9f); // forced to 1.0
        Assert.Equal(a, b);
        Assert.Equal("/GS_ALPHA_1", a);
    }

    [Fact]
    public void RegisterDeferredXObject_NumbersAndDeduplicatesByFontSizeColour()
    {
        PdfResourceRegistry r = new();
        Assert.Equal("/DXO1", r.RegisterDeferredXObject("Helvetica", 12, Color.Black));
        Assert.Equal("/DXO1", r.RegisterDeferredXObject("Helvetica", 12, Color.Black)); // dedup
        Assert.Equal("/DXO2", r.RegisterDeferredXObject("Helvetica", 14, Color.Black)); // different size
    }

    [Fact]
    public void PdfAEnabled_AndConformanceStayInSync()
    {
        PdfResourceRegistry r = new();
        Assert.False(r.PdfAEnabled);
        Assert.Equal(PdfAConformance.None, r.Conformance);

        r.PdfAEnabled = true;
        Assert.Equal(PdfAConformance.PdfA2b, r.Conformance);

        r.Conformance = PdfAConformance.PdfA1b;
        Assert.True(r.PdfAEnabled);

        r.Conformance = PdfAConformance.None;
        Assert.False(r.PdfAEnabled);
    }

    [Fact]
    public void GetEmbeddedFont_ReturnsNullForUnknownPath()
    {
        Assert.Null(new PdfResourceRegistry().GetEmbeddedFont("nope.ttf"));
    }
}
