using BonniePDF.Elements.Text;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Document;

/// <summary>
/// Cancellation of the asynchronous generate, save and SVG export paths.
/// The TypeScript counterpart takes an <c>AbortSignal</c> in the options.
///
/// Mirrors core/tests/document/abort-signal.test.ts.
/// </summary>
public class CancellationTests
{
    private static PdfDocument DocumentWithText()
    {
        var doc = new PdfDocument();
        doc.Add(new TextBlock { Text = "Hello" });
        return doc;
    }

    private static CancellationToken Cancelled()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        return source.Token;
    }

    [Fact]
    public async Task GenerateBytesAsync_ThrowsWhenAlreadyCancelled()
    {
        using PdfDocument doc = DocumentWithText();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doc.GenerateBytesAsync(Cancelled()));
    }

    [Fact]
    public async Task SaveAsync_ThrowsWhenAlreadyCancelled()
    {
        using PdfDocument doc = DocumentWithText();
        string path = Path.Combine(Path.GetTempPath(), "never-written-" + Guid.NewGuid().ToString("N") + ".pdf");
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doc.SaveAsync(path, Cancelled()));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task SvgExportsThrowWhenAlreadyCancelled()
    {
        using PdfDocument doc = DocumentWithText();
        CancellationToken token = Cancelled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doc.GenerateSvgPagesAsync(null, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doc.GenerateSvgDocumentAsync(null, token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => doc.GenerateFirstPageSvgAsync(null, token));
    }
}
