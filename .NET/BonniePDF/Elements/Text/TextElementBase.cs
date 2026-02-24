using System.Collections.Generic;
using BonniePDF.Core;
using BonniePDF.Typography;

namespace BonniePDF.Elements.Text;

/// <summary>
/// Base class for text-based PDF elements that provides common functionality for
/// font handling, text encoding, and line wrapping. This reduces code duplication
/// across text elements and ensures consistent text rendering behavior.
/// </summary>
public abstract class TextElementBase : ElementBase
{
    /// <summary>
    /// Token for injecting total pages count
    /// </summary>
    public const string TotalPagesToken = "{{TotalPages}}";

    /// <summary>
    /// Font name for text rendering
    /// </summary>
    public string FontName { get; set; } = StandardFonts.Helvetica;

    /// <summary>
    /// Font size for text rendering
    /// </summary>
    public int FontSize { get; set; } = 12;

    /// <summary>
    /// TextBlock color for rendering
    /// </summary>
    public Color TextColor { get; set; } = Color.Black;

    /// <summary>
    /// Text decoration (underline, strikethrough, or both)
    /// </summary>
    public TextDecoration TextDecoration { get; set; } = TextDecoration.None;

    /// <summary>
    /// Line height calculated from font size (1.2x font size)
    /// </summary>
    protected float LineHeight => FontSize * 1.2f;

    /// <summary>
    /// Computes wrapped lines for text that exceeds the maximum width.
    /// Uses word wrapping to break lines at appropriate positions.
    /// </summary>
    /// <param name="text">The text to wrap</param>
    /// <param name="maxWidth">The maximum width for a line</param>
    /// <returns>A list of lines, each fitting within the maximum width</returns>
    protected List<string> ComputeLines(string text, float maxWidth)
    {
        List<string> lines = [];
        if (string.IsNullOrEmpty(text)) return lines;

        string[] words = text.Split(' ');
        string currentLine = "";

        foreach (string word in words)
        {
            string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;

            // Calculate exact width of the test line (handling tokens)
            float testWidth = MeasureTextWidth(testLine);

            if (testWidth > maxWidth)
            {
                // Current line is full, add it and start new line with current word
                if (!string.IsNullOrEmpty(currentLine))
                    lines.Add(currentLine);
                currentLine = word;
            }
            else
            {
                // Word fits, add it to current line
                currentLine = testLine;
            }
        }

        // Don't forget the last line
        if (!string.IsNullOrEmpty(currentLine))
            lines.Add(currentLine);

        return lines;
    }

    /// <summary>
    /// Measures the width of a text string using the current font settings.
    /// Handles substitution tokens like {{TotalPages}} by measuring a placeholder.
    /// </summary>
    /// <param name="text">The text to measure</param>
    /// <returns>The width of the text in points</returns>
    protected float MeasureTextWidth(string text)
    {
        // Replace token with estimated placeholder (2 digits) for measurement
        string measuredText = text.Replace(TotalPagesToken, "00");
        return FontMetrics.GetStringWidth(measuredText, FontName, FontSize);
    }
}