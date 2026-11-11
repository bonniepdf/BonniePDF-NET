using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Content;

/// <summary>Spec 03, Table running total: carried- and brought-forward rows across page breaks.</summary>
[Collection("Serial")]
public sealed partial class RunningTotalTests
{
    private static decimal Value(int i) => 12.5m + i;

    private static Table Build(bool running)
    {
        Table table = new(4, 1, 1.4f);
        if (running)
        {
            table.RunningTotalColumn = 2;
            table.RunningTotalPrefix = "£";
        }
        table.AddHeader("Item", "Qty", "Net");
        for (int i = 0; i < 60; i++)
        {
            table.AddRow(new TableRow
            {
                Cells =
                [
                    new TableCell { Content = new TextBlock($"Line {i + 1}") },
                    new TableCell { Content = new TextBlock("1") },
                    new TableCell { Content = new TextBlock(Value(i).ToString("0.00", CultureInfo.InvariantCulture)) },
                ],
                RunningValue = Value(i),
            });
        }
        return table;
    }

    private static string[] PageTexts(bool running)
    {
        using PdfDocument doc = new();
        doc.Add(Build(running));
        return doc.GenerateSvgPages()
            .Select(svg => string.Join(' ', ElementText().Matches(svg).Select(m => m.Groups[1].Value)))
            .ToArray();
    }

    [Fact]
    public void ContinuingPage_EndsWithTheTotalSoFar_AndTheNextOpensWithIt()
    {
        string[] pages = PageTexts(true);
        Assert.True(pages.Length > 1);
        int linesOnFirst = LineLabel().Count(pages[0]);
        decimal sum = Enumerable.Range(0, linesOnFirst).Sum(Value);
        string figure = "£" + sum.ToString("N2", CultureInfo.GetCultureInfo("en-GB"));
        Assert.Contains("Carried forward", pages[0]);
        Assert.Contains(figure, pages[0]);
        Assert.Contains("Brought forward", pages[1]);
        Assert.Contains(figure, pages[1]);
        Assert.True(pages[1].IndexOf("Brought forward", StringComparison.Ordinal)
            < pages[1].IndexOf($"Line {linesOnFirst + 1}", StringComparison.Ordinal));
        Assert.DoesNotContain("Carried forward", pages[^1]);
    }

    private static (int Rows, int Artifacts) Tagged(bool running)
    {
        InternalConfiguration.EnableCompression = false;
        try
        {
            using PdfDocument doc = new();
            doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
            doc.Settings.Language = "en-GB";
            doc.Metadata.Title = "Running total";
            doc.Add(Build(running));
            string pdf = Encoding.Latin1.GetString(doc.GenerateBytes());
            return (RowTag().Count(pdf), ArtifactTag().Count(pdf));
        }
        finally { InternalConfiguration.EnableCompression = true; }
    }

    [Fact]
    public void RunningRows_AreArtifacts_NotTableRows()
    {
        var without = Tagged(false);
        var with = Tagged(true);
        Assert.Equal(without.Rows, with.Rows);
        Assert.True(with.Artifacts > without.Artifacts);
    }

    [Fact]
    public void RunningRows_TakeTheDocumentDefaultTextStyle()
    {
        using PdfDocument doc = new();
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(9);
        doc.Add(Build(true));
        string label = CarriedForwardLabel().Match(doc.GenerateSvgPages()[0]).Value;
        Assert.Matches("font-size=\"9(px)?\"", label);
        Assert.Matches("font-weight=\"(bold|700)\"|Bold", label);
    }

    [Fact]
    public void RunningTotalColumn_ThatLeavesNoRoomForTheLabel_Throws()
    {
        Table table = Build(true);
        table.RunningTotalColumn = 0;
        using PdfDocument doc = new();
        doc.Add(table);
        Exception error = Assert.ThrowsAny<Exception>(() => doc.GenerateBytes());
        Assert.Contains("RunningTotalColumn", error.ToString());
    }

    [GeneratedRegex(">([^<]*)<")]
    private static partial Regex ElementText();

    [GeneratedRegex(@"Line (\d+)")]
    private static partial Regex LineLabel();

    [GeneratedRegex(@"/S /TR\b")]
    private static partial Regex RowTag();

    [GeneratedRegex(@"/Artifact\b")]
    private static partial Regex ArtifactTag();

    [GeneratedRegex("<text[^>]*>Carried forward<")]
    private static partial Regex CarriedForwardLabel();
}
