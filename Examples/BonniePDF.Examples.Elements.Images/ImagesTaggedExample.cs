using System;
using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Images;

/// <summary>
/// PDF/UA-1 + PDF/A-1b tagged example exercising raster images:
///   - an informative <see cref="Image"/> with alt text  -> Figure + /Alt
///   - a decorative <see cref="Image"/> (IsDecorative)    -> Artifact
///
/// Mirror of the TypeScript examples/src/elements/images-tagged.ts. Complements
/// SimpleDocumentTaggedExample (which tags a vector QRCode as a Figure) by
/// validating the embedded raster-image XObject + colour-space path under both
/// flavours. The PNG is generated in-memory so the example has no asset
/// dependency.
///
/// Acceptance: VeraPDF profiles ua1 and 1b report zero failures
/// (tools/verapdf/validate.sh). Wired into Program.cs as images_tagged.pdf.
/// </summary>
[System.ComponentModel.DisplayName("Images (Tagged / PDF-UA-1)")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("PDF/UA-1 tagged images: an informative Figure with alt text and a decorative Artifact.")]
public static class ImagesTaggedExample
{
    public static Document Generate()
    {
        byte[] photoPng = BuildRgbPng(96, (x, y) => ((byte)(x * 255 / 95), (byte)(y * 255 / 95), (byte)128));
        byte[] dividerPng = BuildRgbPng(48, (x, y) => ((byte)220, (byte)224, (byte)230));

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title = "Accessible Images — Tagged (PDF/UA-1)",
                Author = "BonniePDF",
                Subject = "A tagged document with an informative figure and a decorative artifact image.",
                Keywords = "pdf, pdf/ua, accessibility, image, figure, alt-text",
            },
            Settings = new DocumentSettings
            {
                Version = PdfVersion.Pdf17,
                Accessibility = AccessibilityMode.PdfUa1,
                Language = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(Color.Slate700),
            },
        };

        doc.Add(new Heading("Accessible Images")
        {
            Level = 1,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new TextBlock
        {
            Text = "An informative image must carry alternative text so assistive technology can describe it. " +
                   "A purely decorative image must instead be marked as an artifact so it is skipped in the reading order.",
            FontName = StandardFonts.Helvetica,
            Margin = new Thickness(0, 0, 0, 12),
        });

        // Informative image -> Figure + /Alt (alt text present, not decorative).
        doc.Add(new Image
        {
            ImageData = photoPng,
            Width = 200,
            Height = 200,
            FitMode = ImageFitMode.Contain,
            AltText = "A square gradient blending red across the horizontal axis and green down the vertical axis.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        doc.Add(new Heading("Decorative rule")
        {
            Level = 2,
            FontName = StandardFonts.HelveticaBold,
            FontSize = 16,
            TextColor = Color.Slate800,
            Margin = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text = "The light grey bar below is decorative; it is tagged as an artifact and carries no alt text.",
            FontName = StandardFonts.Helvetica,
            Margin = new Thickness(0, 0, 0, 8),
        });

        // Decorative image -> Artifact (IsDecorative => excluded from the structure tree).
        doc.Add(new Image
        {
            ImageData = dividerPng,
            Width = 360,
            Height = 12,
            FitMode = ImageFitMode.Fill,
            IsDecorative = true,
        });

        return doc;
    }

    // ── Minimal self-contained PNG generator ────────────────────────────────
    // A single stored (uncompressed) zlib block keeps this dependency-free;
    // valid for the small images used here. Mirrors images-tagged.ts.

    private static byte[] BuildRgbPng(int size, Func<int, int, (byte R, byte G, byte B)> pixel)
    {
        int stride = 1 + size * 3;
        byte[] raw = new byte[stride * size];
        for (int y = 0; y < size; y++)
        {
            raw[y * stride] = 0; // filter = None
            for (int x = 0; x < size; x++)
            {
                (byte r, byte g, byte b) = pixel(x, y);
                int off = y * stride + 1 + x * 3;
                raw[off] = r;
                raw[off + 1] = g;
                raw[off + 2] = b;
            }
        }

        int nlen = (raw.Length ^ 0xFFFF) & 0xFFFF;
        var idat = new List<byte>
        {
            0x78, 0x01, // zlib header
            0x01,       // BFINAL=1, BTYPE=00 (stored)
            (byte)(raw.Length & 0xFF), (byte)((raw.Length >> 8) & 0xFF),
            (byte)(nlen & 0xFF), (byte)((nlen >> 8) & 0xFF),
        };
        idat.AddRange(raw);
        idat.AddRange(U32Be(Adler32(raw)));

        var ihdr = new List<byte>();
        ihdr.AddRange(U32Be((uint)size));
        ihdr.AddRange(U32Be((uint)size));
        ihdr.AddRange(new byte[] { 8, 2, 0, 0, 0 }); // 8-bit, colour type 2 (RGB)

        var png = new List<byte> { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        png.AddRange(PngChunk("IHDR", ihdr));
        png.AddRange(PngChunk("IDAT", idat));
        png.AddRange(PngChunk("IEND", new List<byte>()));
        return png.ToArray();
    }

    private static List<byte> PngChunk(string type, List<byte> data)
    {
        var typeBytes = new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
        var forCrc = new List<byte>(typeBytes);
        forCrc.AddRange(data);
        var chunk = new List<byte>();
        chunk.AddRange(U32Be((uint)data.Count));
        chunk.AddRange(typeBytes);
        chunk.AddRange(data);
        chunk.AddRange(U32Be(Crc32(forCrc.ToArray())));
        return chunk;
    }

    private static byte[] U32Be(uint n) =>
        new[] { (byte)((n >> 24) & 0xFF), (byte)((n >> 16) & 0xFF), (byte)((n >> 8) & 0xFF), (byte)(n & 0xFF) };

    private static uint Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1, b = 0;
        foreach (byte d in data)
        {
            a = (a + d) % mod;
            b = (b + a) % mod;
        }
        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xFFFFFFFF;
        foreach (byte v in data)
        {
            crc ^= v;
            for (int i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }
        return crc ^ 0xFFFFFFFF;
    }
}
