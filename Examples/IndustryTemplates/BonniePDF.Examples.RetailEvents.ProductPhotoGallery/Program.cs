namespace BonniePDF.Examples.RetailEvents.ProductPhotoGallery;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        var assets = LoadAssets(FindAssetsDir(), ProductPhotoGalleryExample.RequiredAssets);

        Console.WriteLine("Generating Product Photo Gallery...");
        using (var doc = ProductPhotoGalleryExample.Generate(assets))
        {
            doc.Save(Path.Combine(outputDir, "product_photo_gallery.pdf"));
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

    private static Dictionary<string, byte[]> LoadAssets(string assetsDir, IEnumerable<string> paths)
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
