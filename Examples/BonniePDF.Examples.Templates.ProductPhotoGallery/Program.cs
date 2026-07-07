namespace BonniePDF.Examples.Templates.ProductPhotoGallery;

internal sealed class Program
{
    private static void Main()
    {
        Console.WriteLine("Generating Examples...");
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GeneratedPdfs");
        Directory.CreateDirectory(outputDir);

        Console.WriteLine("Generating Product Photo Gallery...");
        using (var doc = ProductPhotoGalleryExample.Generate())
        {
            doc.Save(Path.Combine(outputDir, "product_photo_gallery.pdf"));
        }

        Console.WriteLine("Done! Files saved to " + outputDir);
    }
}
