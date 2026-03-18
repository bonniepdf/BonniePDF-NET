using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Extensions;
using BonniePDF.Fluent;
using BonniePDF.Fluent.Extensions;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Typography.RichTextStyles;

/// <summary>
/// Demonstrates mixed-style rich text using the Fluent API's Text(Action&lt;TextDescriptor&gt;) overload.
/// Each call to text.Span() returns a TextSpanDescriptor whose methods mirror the TextStyle
/// fluent API used in the traditional RichText element.
///
/// Note: Justified alignment on rich text is not exposed through TextDescriptor.
/// Those sections use the escape-hatch column.Item().Element(new RichText { ... }) pattern.
/// </summary>
public static class RichTextStylesFluentExample
{

    public static string Generate(string outputPath)
    {
        Pdf.Create(doc =>
        {
            doc.WithMetadata(metadata =>
            {
                metadata.Title = "Rich Text (Fluent)";
                metadata.Author = "BonniePDF";
            });

            doc.WithSettings(settings =>
            {
                settings.PageSize = PdfSize.A4;
                settings.Margin = 60;
                settings.Version = PdfVersion.Pdf17;
                settings.PdfAEnabled = true;
            });

            // Document default: Helvetica 11pt — spans that don't specify size inherit this
            doc.WithDefaultTextStyle(style => style
                .WithFamily(StandardFonts.Helvetica)
                .WithSize(11));

            doc.Page(page =>
            {
                page.Content().Column(column =>
                {
                    // Title
                    column.Item()
                        .Text("Rich Text (Mixed Styling) — Fluent API")
                        .Bold()
                        .FontSize(24)
                        .TextColor(Color.Slate800)
                        .Margin(0, 0, 0, 20);

                    // ── 1. Bold words ────────────────────────────────────────────
                    SectionHeader(column, "Bold Words in a Paragraph");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("This paragraph contains ");
                            text.Span("bold text").Bold();
                            text.Span(" mixed with regular text. The ");
                            text.Span("bold words").Bold();
                            text.Span(" stand out while the text flows naturally as a single paragraph with automatic word wrapping.");
                        });

                    // ── 2. Italic text ───────────────────────────────────────────
                    SectionHeader(column, "Italic Text");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("Scientific names are typically written in ");
                            text.Span("italic").Italic();
                            text.Span(", such as ");
                            text.Span("Homo sapiens").Italic();
                            text.Span(" or ");
                            text.Span("Escherichia coli").Italic();
                            text.Span(". This convention helps distinguish taxonomic nomenclature from surrounding text.");
                        });

                    // ── 3. Bold + Italic combined ────────────────────────────────
                    SectionHeader(column, "Bold and Italic Combined");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("You can combine ");
                            text.Span("bold").Bold();
                            text.Span(", ");
                            text.Span("italic").Italic();
                            text.Span(", and ");
                            text.Span("bold italic").Bold().Italic();
                            text.Span(" styles within a single flowing paragraph.");
                        });

                    // ── 4. Colored spans ─────────────────────────────────────────
                    SectionHeader(column, "Colored Spans");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("Text can be ");
                            text.Span("red").FontColor(Color.Red);
                            text.Span(", ");
                            text.Span("blue").FontColor(Color.Blue600);
                            text.Span(", ");
                            text.Span("green").FontColor(Color.Green600);
                            text.Span(", or ");
                            text.Span("any color").FontColor(Color.Purple600);
                            text.Span(" you choose. Colors can be combined with ");
                            text.Span("bold colored text").Bold().FontColor(Color.Indigo600);
                            text.Span(" for maximum emphasis.");
                        });

                    // ── 5. Inline decorations ────────────────────────────────────
                    SectionHeader(column, "Inline Decorations");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("Decorations work per-span: ");
                            text.Span("underlined words").Underline();
                            text.Span(" and ");
                            text.Span("strikethrough words").Strikethrough();
                            text.Span(" can appear anywhere in the paragraph, including ");
                            text.Span("bold underlined").Bold().Underline();
                            text.Span(" combinations.");
                        });

                    // ── 6. Mixed font sizes ──────────────────────────────────────
                    SectionHeader(column, "Mixed Font Sizes");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("Normal text can include ");
                            text.Span("larger text").FontSize(18);
                            text.Span(" and ");
                            text.Span("smaller text").FontSize(8);
                            text.Span(" within the same paragraph. The line height adjusts to accommodate the tallest text on each line.");
                        });

                    // ── 7. Background highlights ─────────────────────────────────
                    SectionHeader(column, "Background Highlights");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("You can ");
                            text.Span("highlight important text").BackgroundColor(Color.FromHex("#fef08a"));
                            text.Span(" with background colors, similar to a ");
                            text.Span("marker pen").BackgroundColor(Color.FromHex("#bbf7d0")).Bold();
                            text.Span(" effect. Great for ");
                            text.Span("drawing attention")
                                .BackgroundColor(Color.FromHex("#fecaca"))
                                .FontColor(Color.FromHex("#991b1b"));
                            text.Span(" to key information.");
                        });

                    // ── 8. Reusable TextStyle objects ────────────────────────────
                    // text.Span(string, TextStyle) accepts pre-built styles — useful for
                    // consistent label/value/unit formatting across multiple rows.
                    SectionHeader(column, "Reusable TextStyle Objects");

                    TextStyle label = new() { FontFamily = StandardFonts.Helvetica, Weight = FontWeight.Bold, Color = Color.Slate800 };
                    TextStyle value = new() { FontFamily = StandardFonts.Helvetica, Color = Color.Blue600 };
                    TextStyle unit  = new() { FontFamily = StandardFonts.Helvetica, FontSize = 9, Color = Color.Slate500 };

                    column.Item().Margin(0, 0, 0, 4).Text(text =>
                    {
                        text.Span("Temperature: ", label);
                        text.Span("23.5", value);
                        text.Span(" degrees Celsius", unit);
                    });

                    column.Item().Margin(0, 0, 0, 4).Text(text =>
                    {
                        text.Span("Pressure: ", label);
                        text.Span("1013.25", value);
                        text.Span(" hPa", unit);
                    });

                    column.Item().Margin(0, 0, 0, 16).Text(text =>
                    {
                        text.Span("Humidity: ", label);
                        text.Span("65", value);
                        text.Span(" %", unit);
                    });

                    // ── 9. Font weight variants ──────────────────────────────────
                    SectionHeader(column, "Font Weight Variants");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("Normal (400) ").Weight(FontWeight.Normal);
                            text.Span("Bold (700) ").Weight(FontWeight.Bold);
                            text.Span("Normal again. ").Weight(FontWeight.Normal);
                            text.Span("Standard PDF fonts support Normal and Bold weights. Custom TTF fonts support the full 100–900 range.");
                        });

                    // ── 10. Justified rich text ──────────────────────────────────
                    // TextDescriptor does not expose alignment; use the Element() escape
                    // hatch to set Alignment = TextAlignment.Justify on the RichText directly.
                    SectionHeader(column, "Justified Alignment");

                    column.Item().Element(
                        new RichText
                        {
                            Alignment = TextAlignment.Justify,
                            Margin = new Thickness(0, 0, 0, 16)
                        }
                        .Span("Rich text supports ")
                        .Span("full justification", new TextStyle().Bold())
                        .Span(" where the text is evenly distributed across the line width. This works with ")
                        .Span("mixed styles", new TextStyle().WithItalic())
                        .Span(" — each span maintains its own formatting while the spacing between words is adjusted to fill the available width. The last line remains left-aligned as per standard typographic convention.")
                    );

                    // ── 11. Practical: legal disclaimer ──────────────────────────
                    column.Item().Margin(0, 12, 0, 0).Line(1, Color.Slate300);
                    SectionHeader(column, "Practical Example: Legal Disclaimer");

                    column.Item().Element(
                        new RichText
                        {
                            DefaultStyle = new TextStyle { FontFamily = StandardFonts.Helvetica, FontSize = 10, Color = Color.Slate600 },
                            Alignment = TextAlignment.Justify,
                            Margin = new Thickness(0, 0, 0, 16)
                        }
                        .Span("IMPORTANT: ", new TextStyle().Bold().WithColor(Color.Red))
                        .Span("By using this software, you agree to the ")
                        .Span("Terms of Service", new TextStyle().Bold().Underline())
                        .Span(" and ")
                        .Span("Privacy Policy", new TextStyle().Bold().Underline())
                        .Span(". All data is processed in accordance with ")
                        .Span("GDPR", new TextStyle().Bold())
                        .Span(" regulations. For questions, contact ")
                        .Span("support@example.com", new TextStyle().WithColor(Color.Blue600))
                        .Span(".")
                    );

                    // ── 12. The fluent pattern IS the callback API ───────────────
                    // In the traditional RichText API, callback-based styling was:
                    //   .Span("bold blue", s => s.Bold().WithColor(Color.Blue600))
                    // In the Fluent API this becomes the standard pattern:
                    //   text.Span("bold blue").Bold().FontColor(Color.Blue600)
                    SectionHeader(column, "The Fluent Span Pattern");

                    column.Item()
                        .Margin(0, 0, 0, 16)
                        .Text(text =>
                        {
                            text.Span("In the Fluent API, every span is styled inline — this ");
                            text.Span("bold blue").Bold().FontColor(Color.Blue600);
                            text.Span(", ");
                            text.Span("italic green").Italic().FontColor(Color.Green600);
                            text.Span(", and ");
                            text.Span("underlined red").Underline().FontColor(Color.Red);
                            text.Span(" are all set directly on the TextSpanDescriptor. No callback or TextStyle object needed.");
                        });
                });
            });
        }).Save(outputPath);

        return outputPath;
    }

    private static void SectionHeader(Fluent.Descriptors.ColumnDescriptor column, string title)
    {
        column.Item()
            .Text(title)
            .Bold()
            .FontSize(14)
            .TextColor(Color.Indigo600)
            .Margin(0, 12, 0, 6);
    }
}
