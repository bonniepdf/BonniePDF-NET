using BonniePDF.Tests.Support;
using Xunit;
using Xunit.Abstractions;

namespace BonniePDF.Tests.Performance;

// Allocation is deterministic where time is not, so it can guard performance in CI. These
// generate representative documents and fail when one allocates noticeably more than its budget.
// A change that lowers allocation should lower the budget with it; one that raises it should say
// why in its commit. Budgets carry about 15% headroom over the measured figure.
[Collection("Serial")]
public sealed class AllocationBudgetTests(ITestOutputHelper output)
{
    // Measured on .NET 8 and .NET 10, Linux x64, October 2026; each test prints its figure.
    private const long TableBudget = 1_900_000;   // measured 1.66 MB
    private const long ParagraphBudget = 3_000_000; // measured 2.58 MB

    private static byte[] FontBytes => File.ReadAllBytes(TestFonts.SansRegularPath);

    // 2,000 one-line rows in tables of 500 with repeating headers, as the large-text benchmark.
    private static void Tables(byte[] font)
    {
        var document = new BonniePDF.Document();
        string family = document.Fonts.Add(font).FontName;
        for (int section = 0; section < 4; section++)
        {
            var table = new Table(1) { CellPadding = 0, RepeatHeadersOnNewPage = true };
            table.AddHeader(new TextBlock($"Numbers from {section * 500} to {section * 500 + 499}") { FontName = family, FontSize = 20 });
            for (int i = section * 500; i < section * 500 + 500; i++)
                table.AddRow(new TextBlock($"Number {i}") { FontName = family, FontSize = 10 });
            document.Add(table);
        }
        document.GenerateBytes();
    }

    // 200 wrapped paragraphs of 60 words.
    private static void Paragraphs(byte[] font)
    {
        var document = new BonniePDF.Document();
        string family = document.Fonts.Add(font).FontName;
        string[] words = ["layout", "measures", "each", "element", "before", "it", "flows", "onto", "the", "page"];
        for (int p = 0; p < 200; p++)
            document.Add(new TextBlock(string.Join(' ', Enumerable.Range(0, 60).Select(w => words[(w * 7 + p) % words.Length]))) { FontName = family, FontSize = 11 });
        document.GenerateBytes();
    }

    private long Measure(Action<byte[]> generate)
    {
        byte[] font = FontBytes;
        generate(font); // first use: static tables, caches, JIT
        long before = GC.GetAllocatedBytesForCurrentThread();
        generate(font);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        output.WriteLine($"allocated {allocated:N0} bytes");
        return allocated;
    }

    [Fact]
    public void LongTablesStayWithinTheirAllocationBudget() =>
        Assert.True(Measure(Tables) is var allocated && allocated <= TableBudget, $"{allocated:N0} bytes allocated; budget {TableBudget:N0}");

    [Fact]
    public void WrappedParagraphsStayWithinTheirAllocationBudget() =>
        Assert.True(Measure(Paragraphs) is var allocated && allocated <= ParagraphBudget, $"{allocated:N0} bytes allocated; budget {ParagraphBudget:N0}");
}
