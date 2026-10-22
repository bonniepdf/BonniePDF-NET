namespace BonniePDF.Examples.ShareRankingCharts.ShareAndRanking;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Dot Plot Chart...");
        using (var doc = DotPlotChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "dot_plot_chart_example.pdf"));
        }

        Console.WriteLine("Generating Lollipop Chart...");
        using (var doc = LollipopChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "lollipop_chart_example.pdf"));
        }

        Console.WriteLine("Generating Pie Chart...");
        using (var doc = PieChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "pie_chart_example.pdf"));
        }

        Console.WriteLine("Generating Horizontal Lollipop Chart...");
        using (var doc = HorizontalLollipopChartExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "horizontal_lollipop_chart_example.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}