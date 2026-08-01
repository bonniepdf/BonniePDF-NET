namespace BonniePDF.Examples.Templates.AnnualReportCovers;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        var assets = LoadAssets(FindAssetsDir(), "images/sean-pollock-PhYq704ffdA-unsplash.jpg");

        Console.WriteLine("Generating Annual - Gradient...");
        using (var doc = AnnualReportGradientCoverExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report_gradient_cover.pdf"));
        }

        Console.WriteLine("Generating Annual - Masked...");
        using (var doc = AnnualReportMaskedCoverExample.Generate(assets))
        {
            doc.Save(Path.Combine(outputDir, "annual_report_masked_cover.pdf"));
        }

        Console.WriteLine("Generating Annual - Geometric...");
        using (var doc = AnnualReportGeometricCoverExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "annual_report_geometric_cover.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }

    private static string FindAssetsDir()
    {
        string? dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
    }

    private static Dictionary<string, byte[]> LoadAssets(string assetsDir, params string[] paths)
    {
        var assets = new Dictionary<string, byte[]>();
        foreach (string path in paths)
        {
            string fullPath = Path.Combine(assetsDir, path.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath))
                assets[path] = File.ReadAllBytes(fullPath);
        }
        return assets;
    }
}
