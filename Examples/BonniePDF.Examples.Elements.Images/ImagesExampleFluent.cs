using System.IO.Compression;
using System.Text;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Images;

/// <summary>
/// Demonstrates JPEG and PNG image embedding using the Fluent API.
/// Images are added via the fluent .Image() terminal, with fit mode and size set
/// through .FitContain() / .FitCover() / .FitFill() and .Width() / .Height() extensions.
/// Bordered / padded containers are achieved by chaining .Padding().Background().Border()
/// before .Image(), so the image becomes the child of the styled container.
/// The PNG generation helpers are copied from the traditional example unchanged.
/// </summary>
[System.ComponentModel.DisplayName("Images (Fluent API)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Embedding and sizing raster images using the Fluent API.")]
public static class ImagesExampleFluent
{
    public static Document Generate()
    {
        string jpgPath      = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "owl.jpg");
        string pngPath      = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "test.png");
        string alphaPngPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "img", "alpha_test.png");

        if (!File.Exists(pngPath))      CreateTestPng(pngPath);
        if (!File.Exists(alphaPngPath)) CreateAlphaTestPng(alphaPngPath);

        return Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title  = "Image Support Demo (Fluent API)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.Version     = PdfVersion.Pdf17;
                settings.Conformance = PdfAConformance.PdfA2b;
            });

            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(12));

            // ── Page 1: JPEG ──────────────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("JPEG Image Support")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(new Color(51, 102, 204))
                        .Margin(0, 0, 0, 4);

                    col.Item()
                        .Text("This section demonstrates JPEG image embedding capabilities.")
                        .FontSize(12)
                        .TextColor(new Color(128, 128, 128))
                        .Margin(0, 0, 0, 8);

                    if (File.Exists(jpgPath))
                    {
                        // 1. Contain mode
                        col.Item()
                            .Text("\n1. Contain Mode (Default)")
                            .Font(StandardFonts.HelveticaBold)
                            .FontSize(16)
                            .TextColor(new Color(51, 102, 204));

                        col.Item()
                            .Text("Image scaled to fit within 300×200 bounds while maintaining aspect ratio.")
                            .FontSize(10)
                            .TextColor(new Color(128, 128, 128));

                        col.Item()
                            .Image(jpgPath)
                            .Width(300)
                            .Height(200)
                            .FitContain();

                        // 2. Cover mode
                        col.Item()
                            .Text("\n2. Cover Mode")
                            .Font(StandardFonts.HelveticaBold)
                            .FontSize(16)
                            .TextColor(new Color(51, 102, 204));

                        col.Item()
                            .Text("Image scaled to fill 300×200 bounds, maintaining aspect ratio (may crop).")
                            .FontSize(10)
                            .TextColor(new Color(128, 128, 128));

                        col.Item()
                            .Image(jpgPath)
                            .Width(300)
                            .Height(200)
                            .FitCover();

                        // 3. Fill mode
                        col.Item()
                            .Text("\n3. Fill Mode")
                            .Font(StandardFonts.HelveticaBold)
                            .FontSize(16)
                            .TextColor(new Color(51, 102, 204));

                        col.Item()
                            .Text("Image stretched to fill 300×200 bounds, ignoring aspect ratio.")
                            .FontSize(10)
                            .TextColor(new Color(128, 128, 128));

                        col.Item()
                            .Image(jpgPath)
                            .Width(300)
                            .Height(200)
                            .FitFill();

                        // 4. Image in container with border
                        col.Item()
                            .Text("\n4. Image in Container")
                            .Font(StandardFonts.HelveticaBold)
                            .FontSize(16)
                            .TextColor(new Color(51, 102, 204));

                        col.Item()
                            .Padding(15)
                            .Border(2, new Color(77, 77, 77))
                            .Image(jpgPath)
                            .Width(250)
                            .FitContain();
                    }
                    else
                    {
                        col.Item()
                            .Text("JPEG image (owl.jpg) not found in img folder.")
                            .TextColor(Color.Red);
                    }
                });
            });

            // ── Page 2: PNG ───────────────────────────────────────────────────
            doc.Page(page =>
            {
                page.Content().Column(col =>
                {
                    col.Item()
                        .Text("PNG Image Support")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(24)
                        .TextColor(new Color(51, 153, 102))
                        .Margin(0, 0, 0, 4);

                    col.Item()
                        .Text("This section demonstrates PNG image embedding with transparency support.")
                        .FontSize(12)
                        .TextColor(new Color(128, 128, 128))
                        .Margin(0, 0, 0, 8);

                    // 1. Basic PNG
                    col.Item()
                        .Text("\n1. Basic PNG Image")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(16)
                        .TextColor(new Color(51, 153, 102));

                    col.Item()
                        .Image(pngPath)
                        .Width(200)
                        .FitContain();

                    // 2. PNG in container with border + background
                    col.Item()
                        .Text("\n2. PNG in Container with Border")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(16)
                        .TextColor(new Color(51, 153, 102));

                    col.Item()
                        .Padding(15)
                        .Border(2, new Color(77, 77, 77))
                        .Background(new Color(242, 242, 242))
                        .Image(pngPath)
                        .Width(150)
                        .FitContain();

                    // 3. PNG with alpha transparency
                    col.Item()
                        .Text("\n3. PNG with Alpha Transparency")
                        .Font(StandardFonts.HelveticaBold)
                        .FontSize(16)
                        .TextColor(new Color(51, 153, 102));

                    col.Item()
                        .Text("PNG images can have alpha transparency (soft mask in PDF).")
                        .FontSize(10)
                        .TextColor(new Color(128, 128, 128));

                    col.Item()
                        .Padding(10)
                        .Background(new Color(204, 230, 217))
                        .Image(alphaPngPath)
                        .Width(200)
                        .FitContain();
                });
            });

        }).Build();

    }

    // ── PNG generation helpers (identical to the traditional example) ──────────

    private static void CreateTestPng(string path)
    {
        int width = 100, height = 100;
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        WriteChunk(writer, "IHDR", BitConverter.IsLittleEndian
            ? [0, 0, 0, (byte)width, 0, 0, 0, (byte)height, 8, 2, 0, 0, 0]
            :
            [
                (byte)(width  >> 24), (byte)(width  >> 16), (byte)(width  >> 8), (byte)width,
                (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height,
                8, 2, 0, 0, 0
            ]);

        MemoryStream rawData = new();
        for (int y = 0; y < height; y++)
        {
            rawData.WriteByte(0);
            for (int x = 0; x < width; x++)
            {
                rawData.WriteByte((byte)(x * 255 / width));
                rawData.WriteByte((byte)(y * 255 / height));
                rawData.WriteByte((byte)((x + y) * 255 / (width + height)));
            }
        }

        WriteChunk(writer, "IDAT", CompressZlib(rawData.ToArray()));
        WriteChunk(writer, "IEND", Array.Empty<byte>());

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, ms.ToArray());
    }

    private static void CreateAlphaTestPng(string path)
    {
        int width = 100, height = 100;
        using MemoryStream ms = new();
        using BinaryWriter writer = new(ms);

        writer.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        byte[] ihdr = new byte[13];
        ihdr[3] = (byte)width; ihdr[7] = (byte)height;
        ihdr[8] = 8; ihdr[9] = 6;
        WriteChunk(writer, "IHDR", ihdr);

        MemoryStream rawData = new();
        float cx = width / 2f, cy = height / 2f, maxR = Math.Min(width, height) / 2f;
        for (int y = 0; y < height; y++)
        {
            rawData.WriteByte(0);
            for (int x = 0; x < width; x++)
            {
                float dx = x - cx, dy = y - cy;
                float t  = Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) / maxR);
                rawData.WriteByte((byte)(t * 255));
                rawData.WriteByte((byte)((1 - t) * 100));
                rawData.WriteByte((byte)((1 - t) * 255));
                rawData.WriteByte((byte)((1 - t) * 255));
            }
        }

        WriteChunk(writer, "IDAT", CompressZlib(rawData.ToArray()));
        WriteChunk(writer, "IEND", Array.Empty<byte>());

        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(path, ms.ToArray());
    }

    private static void WriteChunk(BinaryWriter writer, string type, byte[] data)
    {
        int len = data.Length;
        writer.Write((byte)(len >> 24)); writer.Write((byte)(len >> 16));
        writer.Write((byte)(len >> 8));  writer.Write((byte)len);

        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        writer.Write(typeBytes);
        if (data.Length > 0) writer.Write(data);

        byte[] crcInput = new byte[4 + data.Length];
        Array.Copy(typeBytes, 0, crcInput, 0, 4);
        Array.Copy(data,      0, crcInput, 4, data.Length);
        uint crc = Crc32(crcInput);

        writer.Write((byte)(crc >> 24)); writer.Write((byte)(crc >> 16));
        writer.Write((byte)(crc >> 8));  writer.Write((byte)crc);
    }

    private static byte[] CompressZlib(byte[] data)
    {
        using MemoryStream output = new();
        output.WriteByte(0x78); output.WriteByte(0x9C);
        using (DeflateStream deflate = new(output, CompressionLevel.Optimal, leaveOpen: true))
            deflate.Write(data, 0, data.Length);

        uint adler = Adler32(data);
        output.WriteByte((byte)(adler >> 24)); output.WriteByte((byte)(adler >> 16));
        output.WriteByte((byte)(adler >> 8));  output.WriteByte((byte)adler);
        return output.ToArray();
    }

    private static uint Adler32(byte[] data)
    {
        const uint MOD = 65521;
        uint a = 1, b = 0;
        foreach (byte d in data) { a = (a + d) % MOD; b = (b + a) % MOD; }
        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte bv in data)
        {
            crc ^= bv;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}

