using System.IO.Compression;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Images;

/// <summary>
/// Demonstrates Image support (JPEG and PNG) in BonniePDF
/// </summary>
public static class ImageExample
{
    
    public static string Generate(string outputPath)
    {
        // Setup paths
        string jpgPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "owl.jpg");
        string pngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "test.png");
        string alphaPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "alpha_test.png");

        // Create test PNGs if needed
        if (!File.Exists(pngPath)) CreateTestPng(pngPath);
        if (!File.Exists(alphaPngPath)) CreateAlphaTestPng(alphaPngPath);

        Document doc = new();
        doc.Settings.Version = PdfVersion.Pdf17;
        doc.Settings.PdfAEnabled = true;
        doc.Metadata.Title = "Image Support Demo";
        doc.Metadata.Author = "BonniePDF";

        // --- Page 1: JPEG Support ---
        doc.Add(new TextBlock
        {
            Text = "JPEG Image Support",
            FontSize = 24,
            FontName = StandardFonts.HelveticaBold,
            TextColor = new Color(51, 102, 204)
        });

        doc.Add(new TextBlock
        {
            Text = "This section demonstrates JPEG image embedding capabilities.",
            FontSize = 12,
            TextColor = new Color(128, 128, 128)
        });

        if (File.Exists(jpgPath))
        {
            // Example 1: Contain mode (default)
            doc.Add(new TextBlock
            {
                Text = "\n1. Contain Mode (Default)",
                FontSize = 16,
                FontName = StandardFonts.HelveticaBold,
                TextColor = new Color(51, 102, 204)
            });

            doc.Add(new TextBlock
            {
                Text = "Image scaled to fit within 300x200 bounds while maintaining aspect ratio.",
                FontSize = 10,
                TextColor = new Color(128, 128, 128)
            });

            doc.Add(new Image
            {
                FilePath = jpgPath,
                Width = 300,
                Height = 200,
                FitMode = ImageFitMode.Contain
            });

            // Example 2: Cover mode
            doc.Add(new TextBlock
            {
                Text = "\n2. Cover Mode",
                FontSize = 16,
                FontName = StandardFonts.HelveticaBold,
                TextColor = new Color(51, 102, 204)
            });

            doc.Add(new TextBlock
            {
                Text = "Image scaled to fill 300x200 bounds, maintaining aspect ratio (may crop).",
                FontSize = 10,
                TextColor = new Color(128, 128, 128)
            });

            doc.Add(new Image
            {
                FilePath = jpgPath,
                Width = 300,
                Height = 200,
                FitMode = ImageFitMode.Cover
            });

            // Example 3: Fill mode
            doc.Add(new TextBlock
            {
                Text = "\n3. Fill Mode",
                FontSize = 16,
                FontName = StandardFonts.HelveticaBold,
                TextColor = new Color(51, 102, 204)
            });

            doc.Add(new TextBlock
            {
                Text = "Image stretched to fill 300x200 bounds, ignoring aspect ratio.",
                FontSize = 10,
                TextColor = new Color(128, 128, 128)
            });

            doc.Add(new Image
            {
                FilePath = jpgPath,
                Width = 300,
                Height = 200,
                FitMode = ImageFitMode.Fill
            });

            // Example 4: Image in container with border
            // Moved to next page if running out of space, but let's see.
            // Just add it here.
            doc.Add(new TextBlock
            {
                Text = "\n4. Image in Container",
                FontSize = 16,
                FontName = StandardFonts.HelveticaBold,
                TextColor = new Color(51, 102, 204)
            });

            doc.Add(new Container
            {
                Padding = new Thickness(15),
                Border = new Border(2),
                BorderColors = new BorderColors(new Color(77, 77, 77)),
                Child = new Image
                {
                    FilePath = jpgPath,
                    Width = 250,
                    FitMode = ImageFitMode.Contain
                }
            });
        }
        else
        {
            doc.Add(new TextBlock { Text = "JPEG image (owl.jpg) not found in img folder.", TextColor = Color.Red });
        }

        // --- Page 2: PNG Support ---
        doc.AddPage();

        doc.Add(new TextBlock
        {
            Text = "PNG Image Support",
            FontSize = 24,
            FontName = StandardFonts.HelveticaBold,
            TextColor = new Color(51, 153, 102)
        });

        doc.Add(new TextBlock
        {
            Text = "This section demonstrates PNG image embedding with transparency support.",
            FontSize = 12,
            TextColor = new Color(128, 128, 128)
        });

        // Example 1: Basic PNG
        doc.Add(new TextBlock
        {
            Text = "\n1. Basic PNG Image",
            FontSize = 16,
            FontName = StandardFonts.HelveticaBold,
            TextColor = new Color(51, 153, 102)
        });

        doc.Add(new Image
        {
            FilePath = pngPath,
            Width = 200,
            FitMode = ImageFitMode.Contain
        });

        // Example 2: PNG in container
        doc.Add(new TextBlock
        {
            Text = "\n2. PNG in Container with Border",
            FontSize = 16,
            FontName = StandardFonts.HelveticaBold,
            TextColor = new Color(51, 153, 102)
        });

        doc.Add(new Container
        {
            Padding = new Thickness(15),
            Border = new Border(2),
            BorderColors = new BorderColors(new Color(77, 77, 77)),
            BackgroundColor = new Color(242, 242, 242),
            Child = new Image
            {
                FilePath = pngPath,
                Width = 150,
                FitMode = ImageFitMode.Contain
            }
        });

        // Example 3: PNG with Alpha Transparency
        doc.Add(new TextBlock
        {
            Text = "\n3. PNG with Alpha Transparency",
            FontSize = 16,
            FontName = StandardFonts.HelveticaBold,
            TextColor = new Color(51, 153, 102)
        });

        doc.Add(new TextBlock
        {
            Text = "PNG images can have alpha transparency (soft mask in PDF).",
            FontSize = 10,
            TextColor = new Color(128, 128, 128)
        });

        doc.Add(new Container
        {
            Padding = new Thickness(10),
            BackgroundColor = new Color(204, 230, 217), // Light green background
            Child = new Image
            {
                FilePath = alphaPngPath,
                Width = 200,
                FitMode = ImageFitMode.Contain
            }
        });

        doc.Save(outputPath);
        return outputPath;
    }

    /// <summary>
    /// Creates a simple test PNG (RGB, no alpha)
    /// </summary>
    private static void CreateTestPng(string path)
    {
        // Create a simple 100x100 gradient PNG
        int width = 100;
        int height = 100;

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        // PNG signature
        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk
        WriteChunk(writer, "IHDR", BitConverter.IsLittleEndian
            ? new byte[]
            {
                0, 0, 0, (byte)width, // width (big endian)
                0, 0, 0, (byte)height, // height (big endian)
                8, // bit depth
                2, // color type (RGB)
                0, // compression
                0, // filter method
                0 // interlace
            }
            : new byte[]
            {
                (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width,
                (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
                8, 2, 0, 0, 0
            });

        // Create pixel data with filter bytes
        MemoryStream rawData = new();
        for (int y = 0; y < height; y++)
        {
            rawData.WriteByte(0); // Filter type: None
            for (int x = 0; x < width; x++)
            {
                // Create a gradient pattern
                byte r = (byte)(x * 255 / width);
                byte g = (byte)(y * 255 / height);
                byte b = (byte)((x + y) * 255 / (width + height));
                rawData.WriteByte(r);
                rawData.WriteByte(g);
                rawData.WriteByte(b);
            }
        }

        // Compress pixel data
        byte[] compressedData = CompressZlib(rawData.ToArray());

        // IDAT chunk
        WriteChunk(writer, "IDAT", compressedData);

        // IEND chunk
        WriteChunk(writer, "IEND", Array.Empty<byte>());

        File.WriteAllBytes(path, ms.ToArray());
    }

    /// <summary>
    /// Creates a PNG with alpha transparency (RGBA)
    /// </summary>
    private static void CreateAlphaTestPng(string path)
    {
        // Create a 100x100 PNG with a circular gradient and alpha
        int width = 100;
        int height = 100;

        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        // PNG signature
        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        // IHDR chunk - note we need big-endian width/height
        byte[] ihdrData = new byte[13];
        ihdrData[0] = 0;
        ihdrData[1] = 0;
        ihdrData[2] = 0;
        ihdrData[3] = (byte)width; // width
        ihdrData[4] = 0;
        ihdrData[5] = 0;
        ihdrData[6] = 0;
        ihdrData[7] = (byte)height; // height
        ihdrData[8] = 8; // bit depth
        ihdrData[9] = 6; // color type (RGBA)
        ihdrData[10] = 0; // compression
        ihdrData[11] = 0; // filter method
        ihdrData[12] = 0; // interlace

        WriteChunk(writer, "IHDR", ihdrData);

        // Create RGBA pixel data
        MemoryStream rawData = new();
        float centerX = width / 2f;
        float centerY = height / 2f;
        float maxRadius = Math.Min(width, height) / 2f;

        for (int y = 0; y < height; y++)
        {
            rawData.WriteByte(0); // Filter type: None
            for (int x = 0; x < width; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                // Radial gradient from blue (center) to red (edge)
                float t = Math.Min(1f, distance / maxRadius);

                byte r = (byte)(t * 255);
                byte g = (byte)((1 - t) * 100);
                byte b = (byte)((1 - t) * 255);

                // Alpha: full opacity in center, fading to transparent at edge
                byte a = (byte)((1 - t) * 255);

                rawData.WriteByte(r);
                rawData.WriteByte(g);
                rawData.WriteByte(b);
                rawData.WriteByte(a);
            }
        }

        // Compress pixel data
        byte[] compressedData = CompressZlib(rawData.ToArray());

        // IDAT chunk
        WriteChunk(writer, "IDAT", compressedData);

        // IEND chunk
        WriteChunk(writer, "IEND", Array.Empty<byte>());

        File.WriteAllBytes(path, ms.ToArray());
    }

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        // Length (big-endian)
        int len = data.Length;
        writer.Write((byte)(len >> 24));
        writer.Write((byte)(len >> 16));
        writer.Write((byte)(len >> 8));
        writer.Write((byte)len);

        // Type
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes);

        // Data
        if (data.Length > 0)
            writer.Write(data);

        // CRC (of type + data)
        byte[] crcInput = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, crcInput, 0, 4);
        Array.Copy(data, 0, crcInput, 4, data.Length);
        uint crc = Crc32(crcInput);

        writer.Write((byte)(crc >> 24));
        writer.Write((byte)(crc >> 16));
        writer.Write((byte)(crc >> 8));
        writer.Write((byte)crc);
    }

    private static byte[] CompressZlib(byte[] data)
    {
        using MemoryStream output = new();

        // Zlib header
        output.WriteByte(0x78);
        output.WriteByte(0x9C);

        // Deflate compressed data
        using (DeflateStream deflate = new(output, CompressionLevel.Optimal, true))
        {
            deflate.Write(data, 0, data.Length);
        }

        // Adler-32 checksum
        uint adler = Adler32(data);
        output.WriteByte((byte)(adler >> 24));
        output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));
        output.WriteByte((byte)adler);

        return output.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        const uint MOD = 65521;
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % MOD;
            b = (b + a) % MOD;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
                if ((crc & 1) != 0)
                    crc = (crc >> 1) ^ 0xEDB88320;
                else
                    crc >>= 1;
        }

        return crc ^ 0xFFFFFFFF;
    }
}
