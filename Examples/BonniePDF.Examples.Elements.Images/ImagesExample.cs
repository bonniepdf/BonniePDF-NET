using System.Collections.Generic;
using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Media;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Elements.Images;

/// <summary>
/// Raster images — fit modes, containers, alpha transparency, and accessible
/// tagging. PDF/A-2a + PDF/UA-1: every informative image carries alt text so it
/// becomes a Figure with /Alt, and the decorative divider is marked as an
/// artifact so assistive technology skips it.
///
/// The PNGs are generated in-memory (a single stored zlib block keeps this
/// dependency-free), so the example needs no external assets.
/// </summary>
[System.ComponentModel.DisplayName("Images")]
[System.ComponentModel.Category("Elements")]
[System.ComponentModel.Description("Raster image embedding — fit modes, containers, alpha transparency, and accessible Figure/Artifact tagging. PDF/A-2a + PDF/UA-1.")]
public static class ImagesExample
{
    public static Document Generate()
    {
        byte[] gradientPng = BuildRgbPng(100, (x, y) =>
            ((byte)(x * 255 / 99), (byte)(y * 255 / 99), (byte)((x + y) * 255 / 198)));

        byte[] alphaPng = BuildRgbaPng(100, (x, y) =>
        {
            float dx = x - 50;
            float dy = y - 50;
            float t = System.Math.Min(1f, (float)System.Math.Sqrt(dx * dx + dy * dy) / 50f);
            return ((byte)(t * 255), (byte)((1 - t) * 100), (byte)((1 - t) * 255), (byte)((1 - t) * 255));
        });

        byte[] dividerPng = BuildRgbPng(48, (x, y) => ((byte)220, (byte)224, (byte)230));

        Document doc = new()
        {
            Metadata = new DocumentMetadata
            {
                Title    = "Images",
                Author   = "BonniePDF",
                Subject  = "Raster image embedding — fit modes, containers, alpha transparency, and accessible Figure/Artifact tagging. PDF/A-2a + PDF/UA-1.",
                Keywords = "pdf, pdf/ua, pdf/a, accessibility, image, png, figure, alt-text, transparency, fit-mode",
            },
            Settings = new DocumentSettings
            {
                Conformance   = PdfAConformance.PdfA2a,
                Accessibility = AccessibilityMode.PdfUa1,
                Language      = "en-GB",
                DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[700]),
            },
        };

        // ── Title ──────────────────────────────────────────────────────────────

        doc.Add(new Heading("Images")
        {
            TocIdentifier = "intro",
            Level         = 1,
            FontName      = StandardFonts.HelveticaBold,
            FontSize      = 24,
            TextColor     = ColorPalette.Slate[800],
            Margin        = new Thickness(0, 0, 0, 8),
        });

        doc.Add(new TextBlock
        {
            Text   = "BonniePDF embeds PNG images as PDF image XObjects. Under PDF/UA-1 every image must be either a " +
                     "Figure carrying alternative text (informative) or an Artifact (decorative). Each informative image " +
                     "below carries alt text; the divider at the end is marked decorative.",
            Margin = new Thickness(0, 0, 0, 12),
        });

        // ── Fit modes ────────────────────────────────────────────────────────────

        RenderSection(doc, "Fit Modes", "s-fit",
            FitModeBlock(gradientPng, ImageFitMode.Contain, "Contain", "scaled to fit within the box, preserving aspect ratio"),
            FitModeBlock(gradientPng, ImageFitMode.Cover, "Cover", "scaled to fill the box, preserving aspect ratio (may crop)"),
            FitModeBlock(gradientPng, ImageFitMode.Fill, "Fill", "stretched to fill the box, ignoring aspect ratio"));

        // ── Alpha transparency ───────────────────────────────────────────────────

        RenderSection(doc, "Alpha Transparency", "s-alpha",
            new IElement[]
            {
                Caption("PNG images can carry an alpha channel (a PDF soft mask), letting the background show through."),
                new Image
                {
                    ImageData = alphaPng,
                    Width     = 200,
                    FitMode   = ImageFitMode.Contain,
                    AltText   = "A circular blue-to-red gradient fading to transparent at the edges, on the page background.",
                    Margin    = new Thickness(0, 0, 0, 10),
                },
            },
            new IElement[]
            {
                Caption("The same image inside a tinted container — the alpha reveals the container colour."),
                new Container
                {
                    BackgroundColor = ColorPalette.Emerald[100],
                    Padding         = new Thickness(10),
                    Child = new Image
                    {
                        ImageData = alphaPng,
                        Width     = 200,
                        FitMode   = ImageFitMode.Contain,
                        AltText   = "The circular gradient over a green container, whose colour shows through the transparent edges.",
                    },
                },
            });

        // ── Image in a bordered container ────────────────────────────────────────

        RenderSection(doc, "Image in a Bordered Container", "s-container",
            new IElement[]
            {
                Caption("A bordered, padded container wraps the image."),
                new Container
                {
                    Padding      = new Thickness(15),
                    Border       = new Border(2),
                    BorderColors = new BorderColors(ColorPalette.Slate[600]),
                    Child = new Image
                    {
                        ImageData = gradientPng,
                        Width     = 250,
                        FitMode   = ImageFitMode.Contain,
                        AltText   = "Colour gradient inside a bordered container.",
                    },
                },
            });

        // ── Decorative image ──────────────────────────────────────────────────────

        RenderSection(doc, "Decorative Images", "s-decorative",
            new IElement[]
            {
                Caption("The light grey bar below is decorative; it is tagged as an artifact and carries no alt text."),
                new Image
                {
                    ImageData    = dividerPng,
                    Width        = 360,
                    Height       = 12,
                    FitMode      = ImageFitMode.Fill,
                    IsDecorative = true,
                },
            });

        return doc;
    }

    /// <summary>
    /// Renders a section as a heading followed by one or more blocks. Each block is
    /// wrapped in a Container so a bare Image (which has no pagination of its own and
    /// would be clipped at the page edge) breaks to the next page as a whole unit
    /// with its caption. The heading is grouped with the first block so it never
    /// dangles alone at the bottom of a page while its content flows over.
    /// </summary>
    private static void RenderSection(Document doc, string title, string tocId, params IElement[][] blocks)
    {
        Heading heading = SectionHeading(title, tocId);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (i == 0)
            {
                var withHeading = new List<IElement> { heading };
                withHeading.AddRange(blocks[i]);
                Group(doc, withHeading.ToArray());
            }
            else
            {
                Group(doc, blocks[i]);
            }
        }
    }

    /// <summary>Wraps elements in a Container so they measure and page-break as one unit.</summary>
    private static void Group(Document doc, params IElement[] elements)
    {
        var stack = new VerticalStack();
        foreach (IElement el in elements) stack.AddChild(el);
        doc.Add(new Container { Child = stack });
    }

    private static Heading SectionHeading(string title, string tocId) => new(title)
    {
        TocIdentifier = tocId,
        Level         = 2,
        FontName      = StandardFonts.HelveticaBold,
        FontSize      = 15,
        TextColor     = ColorPalette.Indigo[600],
        Margin        = new Thickness(0, 14, 0, 6),
    };

    private static TextBlock Caption(string text) => new()
    {
        Text      = text,
        FontSize  = 10,
        TextColor = ColorPalette.Slate[500],
        Margin    = new Thickness(0, 0, 0, 6),
    };

    private static IElement[] FitModeBlock(byte[] png, ImageFitMode fitMode, string label, string description) =>
        new IElement[]
        {
            Caption($"{label}: {description}."),
            new Image
            {
                ImageData = png,
                Width     = 300,
                Height    = 150,
                FitMode   = fitMode,
                AltText   = $"Colour gradient shown in {label.ToLowerInvariant()} fit mode.",
                Margin    = new Thickness(0, 0, 0, 10),
            },
        };

    // ── In-memory PNG builder (single stored zlib block; no external deps) ──────

    private static byte[] BuildRgbPng(int size, System.Func<int, int, (byte R, byte G, byte B)> pixel) =>
        BuildPng(size, false, (x, y) =>
        {
            (byte r, byte g, byte b) = pixel(x, y);
            return (r, g, b, 255);
        });

    private static byte[] BuildRgbaPng(int size, System.Func<int, int, (byte R, byte G, byte B, byte A)> pixel) =>
        BuildPng(size, true, pixel);

    private static byte[] BuildPng(int size, bool rgba, System.Func<int, int, (byte R, byte G, byte B, byte A)> pixel)
    {
        int bpp = rgba ? 4 : 3;
        int stride = 1 + size * bpp;
        byte[] raw = new byte[stride * size];
        for (int y = 0; y < size; y++)
        {
            raw[y * stride] = 0; // filter = None
            for (int x = 0; x < size; x++)
            {
                (byte r, byte g, byte b, byte a) = pixel(x, y);
                int off = y * stride + 1 + x * bpp;
                raw[off] = r;
                raw[off + 1] = g;
                raw[off + 2] = b;
                if (rgba) raw[off + 3] = a;
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
        ihdr.AddRange(new byte[] { 8, (byte)(rgba ? 6 : 2), 0, 0, 0 });

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
