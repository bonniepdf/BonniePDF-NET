using BonniePDF.Configuration;
using BonniePDF.Core;
using BonniePDF.Elements.Layout;
using BonniePDF.Elements.Text;
using BonniePDF.Prototyping;
using BonniePDF.Typography;

namespace BonniePDF.Examples.Layout.HorizontalStacks;

/// <summary>
/// Demonstrates the HorizontalStack element for horizontal layouts
/// </summary>
[System.ComponentModel.DisplayName("Horizontal Stacks")]
[System.ComponentModel.Category("Layout")]
[System.ComponentModel.Description("Demonstrates flex-like horizontal layout blocks.")]
public static class HorizontalStacksExample
{
    
    public static Document Generate()
    {
        Document doc = new();

        doc.Settings.Conformance = PdfAConformance.PdfA2a;
        doc.Settings.Accessibility = AccessibilityMode.PdfUa1;
        doc.Settings.Language = "en-GB";
        doc.Settings.DefaultTextStyle = new TextStyle().WithFamily("Helvetica").WithSize(12).WithColor(ColorPalette.Slate[600]);

        // Set document metadata
        doc.Metadata.Title = "Horizontal Stack Example";
        doc.Metadata.Author = "BonniePDF";

        // Title
        doc.Add(new TextBlock
        {
            Text = "Horizontal Stack Examples",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 0, 0, 12)
        });

        // Equal width distribution
        doc.Add(new TextBlock
        {
            Text = "Equal Width Distribution",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 8)
        });

        HorizontalStack equalStack = new()
        {
            Spacing = 10,
            WidthDistribution = WidthDistribution.Equal
        };
        equalStack.AddChild(new TextBlock
        {
            Text = "Item 1",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        });
        equalStack.AddChild(new TextBlock
        {
            Text = "Item 2",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        });
        equalStack.AddChild(new TextBlock
        {
            Text = "Item 3",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        });

        // Wrap in containers with background colors
        Container wrappedEqualStack = new()
        {
            Child = equalStack,
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[200]
        };

        doc.Add(wrappedEqualStack);

        // Weighted width distribution
        doc.Add(new TextBlock
        {
            Text = "Weighted Width Distribution (1:2:3 ratio)",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 16, 0, 8)
        });

        HorizontalStack weightedStack = new()
        {
            Spacing = 10,
            WidthDistribution = WidthDistribution.Weighted
        };
        weightedStack.AddChild(new TextBlock
        {
            Text = "25%",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        }, 1f);
        weightedStack.AddChild(new TextBlock
        {
            Text = "50%",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        }, 2f);
        weightedStack.AddChild(new TextBlock
        {
            Text = "75%",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = Color.White
        }, 3f);

        Container wrappedWeightedStack = new()
        {
            Child = weightedStack,
            Padding = new Thickness(10),
            BackgroundColor = ColorPalette.Slate[200]
        };

        doc.Add(wrappedWeightedStack);

        // Center alignment
        doc.Add(new TextBlock
        {
            Text = "Center Vertical Alignment",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 16, 0, 8)
        });

        HorizontalStack centerStack = new()
        {
            Spacing = 10,
            Alignment = HorizontalAlignment.Center,
            WidthDistribution = WidthDistribution.Equal,
            Height = 160
        };
        centerStack.AddChild(new TextBlock
        {
            Text = "Small",
            FontName = StandardFonts.Helvetica,
            FontSize = 10
        });
        centerStack.AddChild(new TextBlock
        {
            Text = "Medium\n(2 lines)",
            FontName = StandardFonts.Helvetica,
            FontSize = 12
        });
        centerStack.AddChild(new TextBlock
        {
            Text = "Large\n(3 lines)\nof text",
            FontName = StandardFonts.Helvetica,
            FontSize = 14
        });

        doc.Add(centerStack);

        // Colored containers in horizontal stack
        doc.Add(new TextBlock
        {
            Text = "Colored Containers",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 16, 0, 8)
        });

        HorizontalStack colorStack = new()
        {
            Spacing = 5,
            WidthDistribution = WidthDistribution.Equal
        };
        colorStack.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "Red", FontName = StandardFonts.HelveticaBold, FontSize = 14, TextColor = Color.White },
            Padding = new Thickness(15),
            BackgroundColor = Color.Red
        });
        colorStack.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "Green", FontName = StandardFonts.HelveticaBold, FontSize = 14, TextColor = Color.White },
            Padding = new Thickness(15),
            BackgroundColor = ColorPalette.Green[600]
        });
        colorStack.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "Blue", FontName = StandardFonts.HelveticaBold, FontSize = 14, TextColor = Color.White },
            Padding = new Thickness(15),
            BackgroundColor = Color.Blue
        });

        doc.Add(colorStack);

        // Add a second page
        //doc.AddPage();

        doc.Add(new TextBlock
        {
            Text = "Practical Examples",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 24,
            TextColor = ColorPalette.Slate[800],
            Margin = new Thickness(0, 24, 0, 12)
        });

        // Key-value pairs
        doc.Add(new TextBlock
        {
            Text = "Key-Value Pairs",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 0, 0, 8)
        });

        var kvSource = Placeholders.WithSeed(300);

        HorizontalStack kvStack = new()
        {
            Spacing = 10,
            WidthDistribution = WidthDistribution.Weighted
        };
        kvStack.AddChild(new TextBlock
        {
            Text = "Name:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600]
        }, 1f);
        kvStack.AddChild(new TextBlock
        {
            Text = kvSource.Name(),
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[800]
        }, 3f);

        doc.Add(kvStack);

        HorizontalStack kvStack2 = new()
        {
            Spacing = 10,
            WidthDistribution = WidthDistribution.Weighted,
            Margin = new Thickness(0, 8, 0, 0)
        };
        kvStack2.AddChild(new TextBlock
        {
            Text = "Email:",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600]
        }, 1f);
        kvStack2.AddChild(new TextBlock
        {
            Text = kvSource.Email(),
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[800]
        }, 3f);

        doc.Add(kvStack2);

        // Progress bar style
        doc.Add(new TextBlock
        {
            Text = "Progress Bar Style",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 16, 0, 8)
        });

        HorizontalStack progressBar = new()
        {
            Spacing = 0,
            WidthDistribution = WidthDistribution.Weighted
        };
        progressBar.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "75%", FontName = StandardFonts.HelveticaBold, FontSize = 12, TextColor = Color.White },
            Padding = new Thickness(10, 5, 10, 5),
            BackgroundColor = ColorPalette.Green[600],
            Height = 24
        }, 75f);
        progressBar.AddChild(new Container
        {
            Padding = new Thickness(10, 5, 10, 5),
            BackgroundColor = ColorPalette.Slate[300],
            Height = 24
        }, 25f);

        doc.Add(progressBar);

        // Badge style
        doc.Add(new TextBlock
        {
            Text = "Badge Style Layout",
            FontName = StandardFonts.HelveticaBold,
            FontSize = 14,
            TextColor = ColorPalette.Slate[600],
            Margin = new Thickness(0, 16, 0, 8)
        });

        HorizontalStack badgeStack = new()
        {
            Spacing = 10,
            Alignment = HorizontalAlignment.Center
        };
        badgeStack.AddChild(new TextBlock
        {
            Text = "Status:",
            FontName = StandardFonts.Helvetica,
            FontSize = 12,
            TextColor = ColorPalette.Slate[600]
        });
        badgeStack.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "Active", FontName = StandardFonts.HelveticaBold, FontSize = 11, TextColor = Color.White },
            Padding = new Thickness(8, 4, 8, 4),
            BackgroundColor = ColorPalette.Green[600]
        });
        badgeStack.AddChild(new Container
        {
            Child = new TextBlock
                { Text = "Verified", FontName = StandardFonts.HelveticaBold, FontSize = 11, TextColor = Color.White },
            Padding = new Thickness(8, 4, 8, 4),
            BackgroundColor = Color.Blue
        });

        doc.Add(badgeStack);

        // Save the document
        return doc;
    }
}

