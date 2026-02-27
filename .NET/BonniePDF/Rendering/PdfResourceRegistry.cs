using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using BonniePDF.Rendering.Images;
using BonniePDF.Rendering.Writers;
using BonniePDF.Typography;
using BonniePDF.Typography.TrueType;
using BonniePDF.Core;
using BonniePDF.Rendering.Pdf;

namespace BonniePDF.Rendering;

/// <summary>
/// Manages PDF resources (fonts, images, XObjects) for a document.
/// Tracks used resources during rendering and handles their serialization.
/// </summary>
public class PdfResourceRegistry
{
    private static readonly Random _random = new();

    private int _fontCounter;
    private int _imageCounter;

    /// <summary>
    /// Gets or sets whether PDF/A compliance is enabled.
    /// When true, standard font names are automatically resolved to embedded TTF fonts.
    /// </summary>
    public bool PdfAEnabled { get; set; }

    internal Dictionary<string, string> FontAliasMap { get; } = new();
    
    /// <summary>
    /// Maps font identifiers to their PDF resource name (e.g., /F1).
    /// </summary>
    public Dictionary<string, string> FontNameMap { get; } = new();
    
    /// <summary>
    /// Maps font identifiers to their PDF object ID.
    /// </summary>
    public Dictionary<string, int> FontIdMap { get; } = new();

    /// <summary>
    /// Tracks embedded fonts for subsetting.
    /// </summary>
    public Dictionary<string, EmbeddedFontInfo> EmbeddedFonts { get; } = new();

    /// <summary>
    /// Stores loaded images.
    /// </summary>
    public Dictionary<string, ImageInfo> LoadedImages { get; } = new();
    
    /// <summary>
    /// Maps image keys to their internal integer ID.
    /// </summary>
    public Dictionary<string, int> ImageIdMap { get; } = new();
    
    /// <summary>
    /// Maps internal image IDs to their resource name (e.g., /I1).
    /// </summary>
    public Dictionary<int, string> ImageResourceNames { get; } = new();

    // Map from ImageIdMap key to actual PDF object ID
    private readonly Dictionary<string, int> _imagePdfObjectIds = new();

    /// <summary>
    /// Stores deferred form XObjects like Total Pages indicators.
    /// </summary>
    public Dictionary<string, DeferredXObjectInfo> DeferredXObjects { get; } = new();

    /// <summary>
    /// Registers a font and returns its resource name (e.g., /F1).
    /// </summary>
    public string RegisterFont(string fontIdentifier)
    {
        if (IsEmbeddedFont(fontIdentifier)) return RegisterEmbeddedFont(fontIdentifier);

        if (PdfAEnabled)
        {
            string ttfPath = ResolveStandardFontToTtf(fontIdentifier);
            if (ttfPath != null)
            {
                string resourceName = RegisterEmbeddedFont(ttfPath);
                FontNameMap[fontIdentifier] = resourceName;
                FontAliasMap[fontIdentifier] = ttfPath;
                return resourceName;
            }
        }

        if (!FontNameMap.TryGetValue(fontIdentifier, out string value))
        {
            _fontCounter++;
            value = $"/F{_fontCounter}";
            FontNameMap[fontIdentifier] = value;
        }

        return value;
    }

    /// <summary>
    /// Checks if a font is an embedded TrueType/OpenType font.
    /// </summary>
    public static bool IsEmbeddedFont(string fontIdentifier)
    {
        return fontIdentifier.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
               fontIdentifier.EndsWith(".otf", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets embedded font info by file path.
    /// </summary>
    public EmbeddedFontInfo GetEmbeddedFont(string filePath)
    {
        return EmbeddedFonts.TryGetValue(filePath, out EmbeddedFontInfo info) ? info : null;
    }

    private string RegisterEmbeddedFont(string filePath)
    {
        if (!EmbeddedFonts.TryGetValue(filePath, out EmbeddedFontInfo existingFont))
        {
            TtfFont font = new(filePath);
            GlyphUsageTracker tracker = new(font);

            _fontCounter++;
            string resourceName = $"/F{_fontCounter}";
            string subsetPrefix = GenerateSubsetPrefix();

            EmbeddedFonts[filePath] = new EmbeddedFontInfo
            {
                Font = font,
                Tracker = tracker,
                SubsetPrefix = subsetPrefix,
                ResourceName = resourceName
            };

            FontNameMap[filePath] = resourceName;
        }

        return FontNameMap[filePath];
    }

    private static string ResolveStandardFontToTtf(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();
        string[] candidates;
        if (lower.Contains("courier") || lower.Contains("mono"))
            candidates = new[] { "cour.ttf", "consola.ttf" };
        else if (lower.Contains("times"))
            candidates = new[] { "times.ttf", "timesnewroman.ttf" };
        else 
            candidates = new[] { "arial.ttf", "segoeui.ttf", "calibri.ttf" };

        string fontsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts));
        foreach (string candidate in candidates)
        {
            string path = Path.Combine(fontsDir, candidate);
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    private static string GenerateSubsetPrefix()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[_random.Next(chars.Length)])
            .ToArray());
    }

    internal TtfFont ResolveEmbeddedFont(string fontIdentifier)
    {
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(fontIdentifier);
            return fontInfo?.Font;
        }

        if (FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(ttfPath);
            return fontInfo?.Font;
        }

        return null;
    }

    internal void TrackGlyphUsage(string fontIdentifier, string text)
    {
        if (IsEmbeddedFont(fontIdentifier))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(fontIdentifier);
            fontInfo?.Tracker.MarkUsed(text);
            return;
        }

        if (FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath))
        {
            EmbeddedFontInfo fontInfo = GetEmbeddedFont(ttfPath);
            fontInfo?.Tracker.MarkUsed(text);
        }
    }

    /// <summary>
    /// Generates a new image internal ID.
    /// </summary>
    public int GetNewImageId()
    {
        return ++_imageCounter;
    }

    /// <summary>
    /// Registers an image and returns its internal ID, avoiding duplicates based on content hash.
    /// </summary>
    public int RegisterImage(ImageInfo info)
    {
        byte[] dataToHash = info.OriginalData != null && info.OriginalData.Length > 0 ? info.OriginalData : info.Data;
        string key = "img_" + ComputeHash(dataToHash);

        if (!ImageIdMap.TryGetValue(key, out int existingImageId))
        {
            int imageId = GetNewImageId();
            ImageIdMap[key] = imageId;
            LoadedImages[key] = info;
            return imageId;
        }

        return existingImageId;
    }

    private static string ComputeHash(byte[] data)
    {
        byte[] hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    /// <summary>
    /// Gets the resource name for an image ID.
    /// </summary>
    public string GetImageResourceName(int imageId)
    {
        if (!ImageResourceNames.TryGetValue(imageId, out string existingResourceName))
        {
            string resourceName = $"/I{imageId}";
            ImageResourceNames[imageId] = resourceName;
            return resourceName;
        }
        return existingResourceName;
    }

    /// <summary>
    /// Registers a deferred form XObject to be generated later, like total page count placeholders.
    /// </summary>
    public string RegisterDeferredXObject(string fontFamily, float fontSize, Color color)
    {
        string fontResName = RegisterFont(fontFamily);
        string signature = $"{fontResName}_{fontSize}_{color.ToHexString()}";

        if (!DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo info))
        {
            string resName = $"/DXO{DeferredXObjects.Count + 1}";
            info = new DeferredXObjectInfo
            {
                ResourceName = resName,
                FontIdentifier = fontFamily,
                FontResourceName = fontResName,
                FontSize = fontSize,
                Color = color
            };
            DeferredXObjects[signature] = info;
        }

        return info.ResourceName;
    }

    private static string MapToStandardFontName(string fontIdentifier)
    {
        string lower = fontIdentifier.ToLowerInvariant();
        return lower switch
        {
            var s when s.Contains("arial") || s.Contains("helvetica") => StandardFonts.Helvetica,
            var s when s.Contains("times") => StandardFonts.TimesRoman,
            var s when s.Contains("courier") || s.Contains("mono") => StandardFonts.Courier,
            _ => StandardFonts.Helvetica
        };
    }

    private int ResolveFontObjectId(string fontIdentifier)
    {
        if (FontAliasMap.TryGetValue(fontIdentifier, out string ttfPath) &&
            FontIdMap.TryGetValue(ttfPath, out int ttfFontId))
            return ttfFontId;
        return FontIdMap.TryGetValue(fontIdentifier, out int fontId) ? fontId : 0;
    }

    /// <summary>
    /// Writes all resources (fonts, images, deferred xobjects) and returns the /Resources dictionary string.
    /// </summary>
    public string WriteResources(IPdfWriter writer, int totalPages)
    {
        FontWriter fontWriter = new(writer);
        ImageWriter imageWriter = new(writer) { PdfAEnabled = PdfAEnabled };

        // 1. Write Fonts
        foreach (KeyValuePair<string, string> fontEntry in FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            
            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (IsEmbeddedFont(fontIdentifier))
            {
                if (EmbeddedFonts.TryGetValue(fontIdentifier, out EmbeddedFontInfo fontInfo))
                {
                    int type0Id = writer.GetNewId();
                    fontInfo.Type0ObjectId = type0Id;
                    FontIdMap[fontIdentifier] = type0Id;
                    fontWriter.WriteEmbeddedTtfFont(type0Id, fontInfo);
                }
            }
            else
            {
                int fontId = writer.GetNewId();
                FontIdMap[fontIdentifier] = fontId;
                fontWriter.WriteType1Font(fontId, MapToStandardFontName(fontIdentifier));
            }
        }

        // 2. Write Images
        _imagePdfObjectIds.Clear();
        foreach (KeyValuePair<string, ImageInfo> imageEntry in LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            int internalId = ImageIdMap[key];
            string resourceName = GetImageResourceName(internalId);

            imageWriter.WriteImage(pdfObjectId, info, resourceName);
        }

        // 3. Write Deferred XObjects
        Dictionary<string, int> deferredIds = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            deferredIds[kvp.Key] = id;

            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            TtfFont embeddedFont = ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(info.FontIdentifier)} 0 R >> >>"
            };

            writer.WriteObject(id, dict, content);
        }

        // 4. Build Resource String
        List<string> fontRefs = new();
        foreach (KeyValuePair<string, string> fontEntry in FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value;

            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (FontIdMap.TryGetValue(fontIdentifier, out int objectId))
                fontRefs.Add($"{resourceName} {objectId} 0 R");
        }
        string fontResources = "<< /Font << " + string.Join(' ', fontRefs) + " >> >>";

        List<string> xobjRefs = new();
        if (ImageIdMap.Count > 0)
            foreach (KeyValuePair<string, int> imageEntry in ImageIdMap)
            {
                string key = imageEntry.Key;
                int internalId = imageEntry.Value;

                if (_imagePdfObjectIds.TryGetValue(key, out int pdfObjectId))
                {
                    string resourceName = GetImageResourceName(internalId);
                    xobjRefs.Add($"{resourceName} {pdfObjectId} 0 R");
                }
            }
        if (deferredIds.Count > 0)
            foreach (KeyValuePair<string, int> kvp in deferredIds)
            {
                string signature = kvp.Key;
                int objectId = kvp.Value;

                if (DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo info))
                    xobjRefs.Add($"{info.ResourceName} {objectId} 0 R");
            }

        string imageResources = xobjRefs.Count > 0 ? "<< /XObject << " + string.Join(' ', xobjRefs) + " >> >>" : "";
        
        return CombineResources(fontResources, imageResources);
    }

    /// <summary>
    /// Writes all resources (fonts, images, deferred xobjects) asynchronously and returns the /Resources dictionary string.
    /// </summary>
    public async Task<string> WriteResourcesAsync(IAsyncPdfWriter writer, int totalPages, CancellationToken cancellationToken = default)
    {
        FontWriter fontWriter = new(writer);
        ImageWriter imageWriter = new(writer) { PdfAEnabled = PdfAEnabled };

        // 1. Write Fonts
        foreach (KeyValuePair<string, string> fontEntry in FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;

            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (IsEmbeddedFont(fontIdentifier))
            {
                if (EmbeddedFonts.TryGetValue(fontIdentifier, out EmbeddedFontInfo fontInfo))
                {
                    int type0Id = writer.GetNewId();
                    fontInfo.Type0ObjectId = type0Id;
                    FontIdMap[fontIdentifier] = type0Id;
                    await fontWriter.WriteEmbeddedTtfFontAsync(type0Id, fontInfo, true, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                int fontId = writer.GetNewId();
                FontIdMap[fontIdentifier] = fontId;
                await fontWriter.WriteType1FontAsync(fontId, MapToStandardFontName(fontIdentifier), cancellationToken).ConfigureAwait(false);
            }
        }

        // 2. Write Images
        _imagePdfObjectIds.Clear();
        foreach (KeyValuePair<string, ImageInfo> imageEntry in LoadedImages)
        {
            string key = imageEntry.Key;
            ImageInfo info = imageEntry.Value;

            int pdfObjectId = writer.GetNewId();
            _imagePdfObjectIds[key] = pdfObjectId;

            int internalId = ImageIdMap[key];
            string resourceName = GetImageResourceName(internalId);

            await imageWriter.WriteImageAsync(pdfObjectId, info, resourceName, cancellationToken).ConfigureAwait(false);
        }

        // 3. Write Deferred XObjects
        Dictionary<string, int> deferredIds = new();
        foreach (KeyValuePair<string, DeferredXObjectInfo> kvp in DeferredXObjects)
        {
            DeferredXObjectInfo info = kvp.Value;
            int id = writer.GetNewId();
            deferredIds[kvp.Key] = id;

            string text = totalPages.ToString();
            string colorCmd = $"{info.Color.ScR:F3} {info.Color.ScG:F3} {info.Color.ScB:F3} rg";

            TtfFont embeddedFont = ResolveEmbeddedFont(info.FontIdentifier);
            string textOperand;
            if (embeddedFont != null)
            {
                TrackGlyphUsage(info.FontIdentifier, text);
                textOperand = PdfSyntax.ToGlyphIdHexString(text, embeddedFont);
            }
            else
            {
                textOperand = $"({PdfSyntax.Escape(text)})";
            }

            string content =
                $"BT {info.FontResourceName} {info.FontSize} Tf {colorCmd} 0 -{info.FontSize} Td {textOperand} Tj ET";

            Dictionary<string, string> dict = new()
            {
                ["/Type"] = "/XObject",
                ["/Subtype"] = "/Form",
                ["/BBox"] = $"[0 -{info.FontSize * 2} 100 0]",
                ["/Resources"] = $"<< /Font << {info.FontResourceName} {ResolveFontObjectId(info.FontIdentifier)} 0 R >> >>"
            };

            await writer.WriteObjectAsync(id, dict, content, cancellationToken).ConfigureAwait(false);
        }

        // 4. Build Resource String
        List<string> fontRefs = new();
        foreach (KeyValuePair<string, string> fontEntry in FontNameMap)
        {
            string fontIdentifier = fontEntry.Key;
            string resourceName = fontEntry.Value;

            if (FontAliasMap.ContainsKey(fontIdentifier))
                continue;

            if (FontIdMap.TryGetValue(fontIdentifier, out int objectId))
                fontRefs.Add($"{resourceName} {objectId} 0 R");
        }
        string fontResources = "<< /Font << " + string.Join(' ', fontRefs) + " >> >>";

        List<string> xobjRefs = new();
        if (ImageIdMap.Count > 0)
            foreach (KeyValuePair<string, int> imageEntry in ImageIdMap)
            {
                string key = imageEntry.Key;
                int internalId = imageEntry.Value;

                if (_imagePdfObjectIds.TryGetValue(key, out int pdfObjectId))
                {
                    string resourceName = GetImageResourceName(internalId);
                    xobjRefs.Add($"{resourceName} {pdfObjectId} 0 R");
                }
            }
        if (deferredIds.Count > 0)
            foreach (KeyValuePair<string, int> kvp in deferredIds)
            {
                string signature = kvp.Key;
                int objectId = kvp.Value;

                if (DeferredXObjects.TryGetValue(signature, out DeferredXObjectInfo info))
                    xobjRefs.Add($"{info.ResourceName} {objectId} 0 R");
            }

        string imageResources = xobjRefs.Count > 0 ? "<< /XObject << " + string.Join(' ', xobjRefs) + " >> >>" : "";
        
        return CombineResources(fontResources, imageResources);
    }

    private static string CombineResources(string fontResources, string imageResources)
    {
        if (string.IsNullOrEmpty(imageResources))
            return fontResources;

        string imageContent = imageResources;
        if (imageContent.StartsWith("<< "))
            imageContent = imageContent.Substring(3);
        if (imageContent.EndsWith(" >>"))
            imageContent = imageContent.Substring(0, imageContent.Length - 3);

        string fontBase = fontResources;
        if (fontBase.EndsWith(" >>"))
            fontBase = fontBase.Substring(0, fontBase.Length - 3);

        return fontBase + ' ' + imageContent + " >>";
    }
}
