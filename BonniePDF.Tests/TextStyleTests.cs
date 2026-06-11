using BonniePDF.Core;
using BonniePDF.Elements.Text;
using BonniePDF.Typography;
using Xunit;

namespace BonniePDF.Tests;

// TextStyle.MergeWith(base) returns a new style where THIS style's explicitly-set
// values win, and unset (null/Normal/None) values fall back to `base`. These tests
// pin the non-obvious precedence rules around weight/italic/decoration.
public class TextStyleTests
{
    // --- Size and colour inheritance ---

    [Fact]
    public void MergeWith_ExplicitFontSizeOverridesBase()
    {
        TextStyle span = new() { FontSize = 20 };
        Assert.Equal(20, span.MergeWith(new TextStyle { FontSize = 12 }).FontSize);
    }

    [Fact]
    public void MergeWith_UnsetFontSizeInheritsBase()
    {
        Assert.Equal(12, new TextStyle().MergeWith(new TextStyle { FontSize = 12 }).FontSize);
    }

    [Fact]
    public void MergeWith_ColourAndBackgroundInheritWhenUnsetAndOverrideWhenSet()
    {
        TextStyle baseStyle = new() { Color = Color.Black, BackgroundColor = ColorPalette.Amber[400] };

        TextStyle inherits = new TextStyle().MergeWith(baseStyle);
        Assert.Equal(Color.Black, inherits.Color);
        Assert.Equal(ColorPalette.Amber[400], inherits.BackgroundColor);

        TextStyle overrides = new TextStyle { Color = Color.Red }.MergeWith(baseStyle);
        Assert.Equal(Color.Red, overrides.Color);
        Assert.Equal(ColorPalette.Amber[400], overrides.BackgroundColor); // still inherited
    }

    // --- Weight precedence ---

    [Fact]
    public void MergeWith_ExplicitNonNormalWeightWins()
    {
        TextStyle span = new() { Weight = FontWeight.Bold };
        Assert.Equal(FontWeight.Bold, span.MergeWith(new TextStyle()).Weight);
    }

    [Fact]
    public void MergeWith_NormalWeightWithNoFamilyInheritsBaseWeight()
    {
        TextStyle merged = new TextStyle().MergeWith(new TextStyle { Weight = FontWeight.Bold });
        Assert.Equal(FontWeight.Bold, merged.Weight);
    }

    [Fact]
    public void MergeWith_SettingFamilyPinsSpanWeightAndBlocksInheritance()
    {
        // Specifying a family means "use my own weight" — even if it is Normal —
        // so a bold base must NOT bleed into a span that picked a different family.
        TextStyle span = new() { FontFamily = "Times" };
        TextStyle merged = span.MergeWith(new TextStyle { Weight = FontWeight.Bold });
        Assert.Equal(FontWeight.Normal, merged.Weight);
    }

    // --- Italic precedence ---

    [Fact]
    public void MergeWith_ExplicitItalicWins()
    {
        Assert.True(new TextStyle { Italic = true }.MergeWith(new TextStyle()).Italic);
    }

    [Fact]
    public void MergeWith_ItalicInheritedOnlyWhenSpanSetsNoFamily()
    {
        TextStyle italicBase = new() { Italic = true };
        Assert.True(new TextStyle().MergeWith(italicBase).Italic); // inherits
        Assert.False(new TextStyle { FontFamily = "Times" }.MergeWith(italicBase).Italic); // family blocks
    }

    // --- Decoration is replace, not combine ---

    [Fact]
    public void MergeWith_UnsetDecorationInheritsBase()
    {
        TextStyle merged = new TextStyle().MergeWith(new TextStyle { Decoration = TextDecoration.Underline });
        Assert.Equal(TextDecoration.Underline, merged.Decoration);
    }

    [Fact]
    public void MergeWith_SetDecorationReplacesBaseEntirely()
    {
        TextStyle span = new() { Decoration = TextDecoration.Strikethrough };
        TextStyle merged = span.MergeWith(new TextStyle { Decoration = TextDecoration.Underline });
        Assert.Equal(TextDecoration.Strikethrough, merged.Decoration);
        Assert.Equal(TextDecoration.None, merged.Decoration & TextDecoration.Underline); // underline did NOT survive
    }

    // --- Family and edge cases ---

    [Fact]
    public void MergeWith_FamilyOverridesWhenSetAndInheritsWhenUnset()
    {
        Assert.Equal("Times", new TextStyle { FontFamily = "Times" }
            .MergeWith(new TextStyle { FontFamily = "Courier" }).FontFamily);
        Assert.Equal("Courier", new TextStyle()
            .MergeWith(new TextStyle { FontFamily = "Courier" }).FontFamily);
    }

    [Fact]
    public void MergeWith_NullBaseReturnsSameInstance()
    {
        TextStyle span = new() { FontSize = 9 };
        Assert.Same(span, span.MergeWith(null));
    }

    // --- Effective getters and font resolution ---

    [Fact]
    public void Effective_DefaultsAreHelvetica12Black()
    {
        TextStyle s = new();
        Assert.Equal(12f, s.EffectiveFontSize);
        Assert.Equal(Color.Black, s.EffectiveColor);
        Assert.Equal("Helvetica", s.EffectiveFontName);
    }

    [Fact]
    public void EffectiveFontName_MapsWeightAndItalicToStandard14VariantsWithNoFamily()
    {
        Assert.Equal("Helvetica-Bold", new TextStyle { Weight = FontWeight.Bold }.EffectiveFontName);
        Assert.Equal("Helvetica-Oblique", new TextStyle { Italic = true }.EffectiveFontName);
        Assert.Equal("Helvetica-BoldOblique",
            new TextStyle { Weight = FontWeight.Bold, Italic = true }.EffectiveFontName);
    }

    [Fact]
    public void ResolvedFontName_IsNullUntilFamilyChosen()
    {
        Assert.Null(new TextStyle().ResolvedFontName);
        Assert.Equal("Helvetica", new TextStyle { FontFamily = "Helvetica" }.ResolvedFontName);
    }

    // --- Fluent setters ---

    [Fact]
    public void Fluent_WeightShortcutsSetWeight()
    {
        Assert.Equal(FontWeight.Bold, new TextStyle().Bold().Weight);
        Assert.Equal(FontWeight.Light, new TextStyle().Light().Weight);
        Assert.Equal(FontWeight.Thin, new TextStyle().Thin().Weight);
    }

    [Fact]
    public void Fluent_ValueSettersAssignTheirField()
    {
        Assert.True(new TextStyle().WithItalic().Italic);
        Assert.Equal(18, new TextStyle().WithSize(18).FontSize);
        Assert.Equal(Color.Red, new TextStyle().WithColor(Color.Red).Color);
        Assert.Equal(ColorPalette.Amber[400], new TextStyle().WithBackground(ColorPalette.Amber[400]).BackgroundColor);
        Assert.Equal(FontWeight.Bold, new TextStyle().WithWeight(FontWeight.Bold).Weight);
        Assert.Equal("Times", new TextStyle().WithFamily("Times").FontFamily);
        Assert.Equal(1.5f, new TextStyle().WithLineHeight(1.5f).LineHeightMultiplier);
    }

    [Fact]
    public void Fluent_DecorationSettersAreAdditive()
    {
        Assert.Equal(TextDecoration.Underline, new TextStyle().Underline().Decoration);
        TextDecoration both = new TextStyle().Underline().Strikethrough().Decoration;
        Assert.Equal(TextDecoration.Underline, both & TextDecoration.Underline);
        Assert.Equal(TextDecoration.Strikethrough, both & TextDecoration.Strikethrough);
    }

    [Fact]
    public void Fluent_SettersReturnSameInstanceForChaining()
    {
        TextStyle s = new();
        Assert.Same(s, s.Bold());
        Assert.Same(s, s.WithSize(10));
        Assert.Same(s, s.WithColor(Color.Red));
        Assert.Same(s, s.Underline());
    }
}
