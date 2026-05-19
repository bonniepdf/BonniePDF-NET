using Xunit;

namespace BonniePDF.Tests;

public class DocumentTests
{
    [Fact]
    public void CanCreateDocument()
    {
        using Document doc = new();
        Assert.NotNull(doc);
        Assert.Single(doc.Pages);
    }

    [Fact]
    public void CanAddPage()
    {
        using Document doc = new();
        doc.AddPage();
        Assert.Equal(2, doc.Pages.Count);
    }

    [Fact]
    public void CanSaveToStream()
    {
        using Document doc = new();
        using MemoryStream ms = new();

        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        // Check PDF header
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string? line = reader.ReadLine();
        Assert.NotNull(line);
        Assert.StartsWith("%PDF-1.4", line);
    }
}