using Xunit;

namespace BonniePDF.Tests.Metadata;

public class BookmarkTests
{
    [Fact]
    public void AddBookmark_SetsVersionAndAddsBookmark()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.AddBookmark("Chapter 1", 0);

        Assert.Equal(PdfVersion.Pdf17, doc.Settings.Version);
        Assert.Single(doc.Settings.Bookmarks);
        Assert.Equal("Chapter 1", doc.Settings.Bookmarks[0].Title);
        Assert.Equal(0, doc.Settings.Bookmarks[0].PageIndex);
    }

    [Fact]
    public void HasBookmarks_ReturnsTrueWhenBookmarksExist()
    {
        using BonniePDF.Document doc = new();
        Assert.False(doc.Settings.HasBookmarks);

        doc.Settings.AddBookmark("Test", 0);
        Assert.True(doc.Settings.HasBookmarks);
    }

    [Fact]
    public void Bookmarks_RequirePdf17()
    {
        using BonniePDF.Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf14;
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Test", PageIndex = 0 });

        using MemoryStream ms = new();
        Assert.Throws<InvalidOperationException>(() => doc.Save(ms, true));
    }

    [Fact]
    public void Bookmarks_SavesSuccessfully()
    {
        using BonniePDF.Document doc = new();
        doc.AddPage();
        doc.Settings.AddBookmark("Page 1", 0);
        doc.Settings.AddBookmark("Page 2", 1);

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("/Outlines", content);
        Assert.Contains("Page 1", content);
        Assert.Contains("Page 2", content);
    }

    [Fact]
    public void Bookmarks_WithNestedChildren_SavesSuccessfully()
    {
        using BonniePDF.Document doc = new();
        doc.AddPage();
        doc.AddPage();

        var bookmark = new BookmarkItem { Title = "Part 1", PageIndex = 0 };
        bookmark.Children.Add(new BookmarkItem { Title = "Chapter 1.1", PageIndex = 0 });
        bookmark.Children.Add(new BookmarkItem { Title = "Chapter 1.2", PageIndex = 1 });

        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.Bookmarks.Add(bookmark);
        doc.Settings.Bookmarks.Add(new BookmarkItem { Title = "Part 2", PageIndex = 2 });

        using MemoryStream ms = new();
        doc.Save(ms, true);

        Assert.True(ms.Length > 0);
        ms.Position = 0;
        using StreamReader reader = new(ms);
        string content = reader.ReadToEnd();
        Assert.Contains("Part 1", content);
        Assert.Contains("Chapter 1.1", content);
    }
}
