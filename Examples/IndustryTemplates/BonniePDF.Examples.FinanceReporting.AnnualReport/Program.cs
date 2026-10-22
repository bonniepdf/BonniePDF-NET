namespace BonniePDF.Examples.FinanceReporting.AnnualReport;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        string assetsDir = FindAssetsDir();
        var assets = LoadAssets(assetsDir, "images/sean-pollock-PhYq704ffdA-unsplash.jpg");

        Console.WriteLine("Generating Annual - Classic...");
        using (var doc = AnnualReportExample.Generate(assets))
        {
            doc.Save(Path.Combine(outputDir, "annual_report.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }

    private static string FindAssetsDir()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        while (dir != null)
        {
            string candidate = Path.Combine(dir, "Examples", "assets");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir)!;
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