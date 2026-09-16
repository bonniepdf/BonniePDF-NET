using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BonniePDF.Elements.Text;
using BonniePDF.Licensing;
using Xunit;

// The suite has a `BonniePDF.Tests.Document` namespace. A name declared in an
// enclosing namespace hides a using-alias of the same name, so an alias called
// `Document` would still bind to that namespace from inside
// `BonniePDF.Tests.Support`. `PdfDocument` does not collide, so it binds.
using PdfDocument = BonniePDF.Document;

namespace BonniePDF.Tests.Support;

/// <summary>
/// The .NET half of the shared conformance fixture
/// <c>licence-declaration/tier-invariance</c>.
/// </summary>
/// <remarks>
/// The TypeScript suite asserts the same matrix against the same fixture. When a row
/// changes here it changes there, and in the fixture first.
/// <para>
/// In the "Serial" collection: every test here mutates process-wide static state
/// (the declared tier, the check's memo, environment variables), so running in
/// parallel with anything else would make both flaky.
/// </para>
/// </remarks>
[Collection("Serial")]
public sealed partial class LicenseDeclarationTests : IDisposable
{
    private const string LicenseVariable = "BONNIEPDF_LICENSE";
    private const string EnforcementVariable = "BONNIEPDF_LICENSE_ENFORCEMENT";

    private readonly TextWriter _originalError;

    public LicenseDeclarationTests()
    {
        _originalError = Console.Error;
        Environment.SetEnvironmentVariable(LicenseVariable, null);
        Environment.SetEnvironmentVariable(EnforcementVariable, null);
        BonniePdfLicense.Tier = null;
        LicenseChecker.ResetForTesting();
    }

    public void Dispose()
    {
        Console.SetError(_originalError);
        Environment.SetEnvironmentVariable(LicenseVariable, null);
        Environment.SetEnvironmentVariable(EnforcementVariable, null);

        // Restore the assembly-wide declaration made in LicenseSetup, so a test here
        // cannot leave a later test class undeclared.
        BonniePdfLicense.Tier = LicenseTier.Evaluation;
        LicenseChecker.ResetForTesting();
    }

    /// <summary>Runs the check with standard error captured, returning what it wrote.</summary>
    private static (Exception? Thrown, string Warnings) RunCheck(LicenseEnvironmentKind environment)
    {
        StringWriter captured = new();
        TextWriter previous = Console.Error;
        Console.SetError(captured);
        try
        {
            LicenseChecker.ValidateLicense(environment);
            return (null, captured.ToString());
        }
        catch (Exception ex)
        {
            return (ex, captured.ToString());
        }
        finally
        {
            Console.SetError(previous);
        }
    }

    private static PdfDocument FixtureDocument()
    {
        PdfDocument document = new();
        document.Metadata.CreationDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        document.Metadata.ModifiedDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        document.Add(new TextBlock("Licence declaration fixture"));
        return document;
    }

    /// <summary>
    /// Masks the trailer's <c>/ID [&lt;..&gt; &lt;..&gt;]</c> file identifier.
    /// </summary>
    /// <remarks>
    /// Every PDF gets a fresh random identifier, so two generations of the same
    /// document are never literally equal. That randomness is required by PDF/A and
    /// has nothing to do with licensing — the comparison masks it and asserts
    /// equality on everything else, which is exactly the property this fixture is
    /// about.
    /// </remarks>
    private static string WithoutFileIdentifier(byte[] bytes) =>
        FileIdentifierRegex().Replace(Encoding.Latin1.GetString(bytes), "/ID [<MASKED> <MASKED>]");

    [GeneratedRegex(@"/ID \[<[0-9A-F]*> <[0-9A-F]*>\]")]
    private static partial Regex FileIdentifierRegex();

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Byte invariance — the mechanical guard the whole feature rests on.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ProducesByteIdenticalOutputUnderEveryTierAndUndeclared()
    {
        Console.SetError(TextWriter.Null);
        List<(string Label, byte[] Bytes)> outputs = new();

        foreach (LicenseTier tier in Enum.GetValues<LicenseTier>())
        {
            BonniePdfLicense.Tier = tier;
            LicenseChecker.ResetForTesting();
            using PdfDocument document = FixtureDocument();
            outputs.Add((tier.ToString(), document.GenerateBytes()));
        }

        BonniePdfLicense.Tier = null;
        Environment.SetEnvironmentVariable(EnforcementVariable, "warn");
        LicenseChecker.ResetForTesting();
        using (PdfDocument undeclared = FixtureDocument())
        {
            outputs.Add(("undeclared", undeclared.GenerateBytes()));
        }

        Assert.Equal(5, outputs.Count);
        string first = WithoutFileIdentifier(outputs[0].Bytes);
        for (int i = 1; i < outputs.Count; i++)
        {
            Assert.True(
                outputs[0].Bytes.Length == outputs[i].Bytes.Length,
                $"{outputs[i].Label} differs in length from {outputs[0].Label}");
            Assert.Equal(first, WithoutFileIdentifier(outputs[i].Bytes));
        }
    }

    [Fact]
    public void NeverMentionsTheLicenceTierInTheGeneratedBytes()
    {
        BonniePdfLicense.Tier = LicenseTier.Enterprise;
        LicenseChecker.ResetForTesting();
        using PdfDocument document = FixtureDocument();

        string pdf = Encoding.Latin1.GetString(document.GenerateBytes());
        foreach (LicenseTier tier in Enum.GetValues<LicenseTier>())
        {
            Assert.DoesNotContain(tier.ToString(), pdf, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. The outcome matrix.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThrowsInDevelopmentWhenUndeclaredAndSaysWhatTheReaderNeeds()
    {
        (Exception? thrown, string warnings) = RunCheck(LicenseEnvironmentKind.Development);

        BonniePdfLicenseException licenseException = Assert.IsType<BonniePdfLicenseException>(thrown);
        Assert.Empty(warnings);

        // The four binding substrings of the fixture.
        Assert.Contains("[BonniePDF]", licenseException.Message, StringComparison.Ordinal);
        Assert.Contains("1,000,000", licenseException.Message, StringComparison.Ordinal);
        Assert.Contains("Benefiting Organisation", licenseException.Message, StringComparison.Ordinal);
        Assert.Contains("no licence key", licenseException.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LicenseEnvironmentKind.Production)]
    [InlineData(LicenseEnvironmentKind.Unknown)]
    public void WarnsRatherThanThrowingWhenUndeclaredOutsideDevelopment(LicenseEnvironmentKind environment)
    {
        (Exception? thrown, string warnings) = RunCheck(environment);

        Assert.Null(thrown);
        Assert.Contains("[BonniePDF]", warnings, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LicenseEnvironmentKind.Development)]
    [InlineData(LicenseEnvironmentKind.Production)]
    [InlineData(LicenseEnvironmentKind.Unknown)]
    public void WarnsForTheEvaluationTier(LicenseEnvironmentKind environment)
    {
        BonniePdfLicense.Tier = LicenseTier.Evaluation;

        (Exception? thrown, string warnings) = RunCheck(environment);

        Assert.Null(thrown);
        Assert.Contains("[BonniePDF]", warnings, StringComparison.Ordinal);
        Assert.Contains("not permitted for production", warnings, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LicenseTier.Community, LicenseEnvironmentKind.Development)]
    [InlineData(LicenseTier.Community, LicenseEnvironmentKind.Production)]
    [InlineData(LicenseTier.Community, LicenseEnvironmentKind.Unknown)]
    [InlineData(LicenseTier.Professional, LicenseEnvironmentKind.Development)]
    [InlineData(LicenseTier.Professional, LicenseEnvironmentKind.Production)]
    [InlineData(LicenseTier.Professional, LicenseEnvironmentKind.Unknown)]
    [InlineData(LicenseTier.Enterprise, LicenseEnvironmentKind.Development)]
    [InlineData(LicenseTier.Enterprise, LicenseEnvironmentKind.Production)]
    [InlineData(LicenseTier.Enterprise, LicenseEnvironmentKind.Unknown)]
    public void IsSilentForDeclaredPaidAndCommunityTiers(LicenseTier tier, LicenseEnvironmentKind environment)
    {
        BonniePdfLicense.Tier = tier;

        (Exception? thrown, string warnings) = RunCheck(environment);

        Assert.Null(thrown);
        Assert.Empty(warnings);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. The enforcement override.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void DowngradesADevelopmentThrowToAWarningWhenSetToWarn()
    {
        Environment.SetEnvironmentVariable(EnforcementVariable, "warn");

        (Exception? thrown, string warnings) = RunCheck(LicenseEnvironmentKind.Development);

        Assert.Null(thrown);
        Assert.Contains("[BonniePDF]", warnings, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LicenseEnvironmentKind.Production)]
    [InlineData(LicenseEnvironmentKind.Unknown)]
    public void UpgradesAWarningToAThrowWhenSetToThrow(LicenseEnvironmentKind environment)
    {
        Environment.SetEnvironmentVariable(EnforcementVariable, "throw");

        (Exception? thrown, _) = RunCheck(environment);

        Assert.IsType<BonniePdfLicenseException>(thrown);
    }

    [Theory]
    [InlineData("silent")]
    [InlineData("nonsense")]
    [InlineData("")]
    public void CannotSuppressTheNotice(string value)
    {
        // There is deliberately no way to switch the notice off. An unrecognised
        // value falls back to environment detection rather than silencing.
        Environment.SetEnvironmentVariable(EnforcementVariable, value);

        (Exception? thrown, _) = RunCheck(LicenseEnvironmentKind.Development);

        Assert.IsType<BonniePdfLicenseException>(thrown);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Memoisation.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void WarnsOnceAcrossFiftyDocuments()
    {
        Environment.SetEnvironmentVariable(EnforcementVariable, "warn");
        StringWriter captured = new();
        Console.SetError(captured);

        for (int i = 0; i < 50; i++)
        {
            using PdfDocument document = new();
            document.Add(new TextBlock($"Document {i}"));
            document.GenerateBytes();
        }

        Console.SetError(_originalError);
        int occurrences = CountOccurrences(captured.ToString(), "[BonniePDF]");
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void KeepsThrowingOnEveryAttemptWhileUndeclaredInDevelopment()
    {
        // A fatal notice is not "spent" by being shown once: a consumer who ignores
        // the first throw must hit it again, or the memo would turn a hard error
        // into a one-off they never see twice.
        Assert.Throws<BonniePdfLicenseException>(
            () => LicenseChecker.ValidateLicense(LicenseEnvironmentKind.Development));
        Assert.Throws<BonniePdfLicenseException>(
            () => LicenseChecker.ValidateLicense(LicenseEnvironmentKind.Development));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (index >= 0)
        {
            count++;
            index = haystack.IndexOf(needle, index + needle.Length, StringComparison.Ordinal);
        }

        return count;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Environment classification, as a pure function.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TreatsCiAsDevelopmentEvenWhenTheHostEnvironmentSaysProduction()
    {
        EnvironmentSignals signals = new("Production", "true", testRunner: false, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Development, LicenseEnvironment.Classify(signals));
    }

    [Fact]
    public void ClassifiesABareProductionHostEnvironmentAsProduction()
    {
        EnvironmentSignals signals = new("Production", null, testRunner: false, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Production, LicenseEnvironment.Classify(signals));
    }

    [Fact]
    public void ClassifiesAnEmptyEnvironmentAsUnknown()
    {
        EnvironmentSignals signals = new(null, null, testRunner: false, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Unknown, LicenseEnvironment.Classify(signals));
    }

    [Theory]
    [InlineData("false")]
    [InlineData("0")]
    public void DoesNotTreatADisabledCiFlagAsCi(string value)
    {
        EnvironmentSignals signals = new(null, value, testRunner: false, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Unknown, LicenseEnvironment.Classify(signals));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("development")]
    [InlineData("Test")]
    public void ClassifiesDevelopmentAndTestHostEnvironmentsAsDevelopment(string value)
    {
        EnvironmentSignals signals = new(value, null, testRunner: false, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Development, LicenseEnvironment.Classify(signals));
    }

    [Fact]
    public void ClassifiesAHostingTestRunnerAsDevelopment()
    {
        EnvironmentSignals signals = new(null, null, testRunner: true, debuggerAttached: false);
        Assert.Equal(LicenseEnvironmentKind.Development, LicenseEnvironment.Classify(signals));
    }

    [Fact]
    public void ClassifiesAnAttachedDebuggerAsDevelopment()
    {
        EnvironmentSignals signals = new(null, null, testRunner: false, debuggerAttached: true);
        Assert.Equal(LicenseEnvironmentKind.Development, LicenseEnvironment.Classify(signals));
    }

    [Fact]
    public void ReadsTheAmbientEnvironmentWithoutThrowing()
    {
        EnvironmentSignals signals = LicenseEnvironment.ReadSignals();

        // This process is hosting xunit, so the test-runner signal must be positive.
        // That is precisely why the matrix above is asserted against the pure
        // function rather than the ambient reader.
        Assert.True(signals.TestRunner);
        Assert.Equal(LicenseEnvironmentKind.Development, LicenseEnvironment.Classify());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Declaration precedence.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void PrefersTheInCodeDeclarationOverTheEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable(LicenseVariable, "community");
        BonniePdfLicense.Tier = LicenseTier.Professional;

        Assert.Equal(LicenseTier.Professional, LicenseChecker.EffectiveTier());
    }

    [Fact]
    public void FallsBackToTheEnvironmentVariableWhenUnsetInCode()
    {
        Environment.SetEnvironmentVariable(LicenseVariable, "community");

        Assert.Equal(LicenseTier.Community, LicenseChecker.EffectiveTier());
    }

    [Theory]
    [InlineData("COMMUNITY")]
    [InlineData("  community ")]
    [InlineData("Community")]
    public void MatchesTheEnvironmentVariableCaseInsensitively(string value)
    {
        Environment.SetEnvironmentVariable(LicenseVariable, value);

        Assert.Equal(LicenseTier.Community, LicenseChecker.EffectiveTier());
    }

    [Theory]
    [InlineData("not-a-tier")]
    [InlineData("7")]
    public void IgnoresAnUnrecognisedEnvironmentValueRatherThanThrowing(string value)
    {
        // An operational variable must never be able to break generation. "7" is the
        // interesting case: Enum.TryParse accepts a numeric string for any integer,
        // so the parse must be paired with a defined-value check.
        Environment.SetEnvironmentVariable(LicenseVariable, value);

        Assert.Null(LicenseChecker.EffectiveTier());
    }

    [Fact]
    public void SilencesTheNoticeWhenDeclaredOnlyThroughTheEnvironment()
    {
        Environment.SetEnvironmentVariable(LicenseVariable, "community");

        (Exception? thrown, string warnings) = RunCheck(LicenseEnvironmentKind.Development);

        Assert.Null(thrown);
        Assert.Empty(warnings);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 7. Invalid in-code declarations.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void ThrowsAtTheAssignmentForAnUndefinedEnumValue()
    {
        Assert.Throws<InvalidLicenseTierException>(() => BonniePdfLicense.Tier = (LicenseTier)99);
    }

    [Fact]
    public void DoesNotConsumeTheOncePerProcessMemoOnAnInvalidAssignment()
    {
        // The failed assignment is a programming error, not a licensing state: the
        // undeclared notice must still be waiting once the bad cast is fixed.
        Assert.Throws<InvalidLicenseTierException>(() => BonniePdfLicense.Tier = (LicenseTier)99);
        Assert.Throws<BonniePdfLicenseException>(
            () => LicenseChecker.ValidateLicense(LicenseEnvironmentKind.Development));
    }

    [Fact]
    public void AcceptsNullAsUnset()
    {
        BonniePdfLicense.Tier = LicenseTier.Community;
        BonniePdfLicense.Tier = null;

        Assert.Null(BonniePdfLicense.Tier);
    }

    [Fact]
    public void RoundTripsEveryValidTier()
    {
        foreach (LicenseTier tier in Enum.GetValues<LicenseTier>())
        {
            BonniePdfLicense.Tier = tier;
            Assert.Equal(tier, BonniePdfLicense.Tier);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 8. Entry-point coverage — the check runs wherever a document is produced.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void GuardsGenerateBytes()
    {
        using PdfDocument document = OnePage();
        Assert.Throws<BonniePdfLicenseException>(() => document.GenerateBytes());
    }

    [Fact]
    public void GuardsSaveToStream()
    {
        using PdfDocument document = OnePage();
        using MemoryStream stream = new();
        Assert.Throws<BonniePdfLicenseException>(() => document.Save(stream));
    }

    [Fact]
    public async System.Threading.Tasks.Task GuardsGenerateBytesAsync()
    {
        using PdfDocument document = OnePage();
        await Assert.ThrowsAsync<BonniePdfLicenseException>(() => document.GenerateBytesAsync());
    }

    [Fact]
    public void GuardsGenerateSvgPages()
    {
        using PdfDocument document = OnePage();
        Assert.Throws<BonniePdfLicenseException>(() => document.GenerateSvgPages());
    }

    [Fact]
    public void GuardsGenerateSvgDocument()
    {
        using PdfDocument document = OnePage();
        Assert.Throws<BonniePdfLicenseException>(() => document.GenerateSvgDocument());
    }

    [Fact]
    public void LetsEveryEntryPointThroughOnceATierIsDeclared()
    {
        BonniePdfLicense.Tier = LicenseTier.Community;

        // One document per entry point: the serializer assumes a frozen tree, and
        // reusing a generated document across entry points is not a supported shape.
        using (PdfDocument forBytes = OnePage())
        {
            Assert.NotEmpty(forBytes.GenerateBytes());
        }

        using (PdfDocument forSvg = OnePage())
        {
            Assert.Single(forSvg.GenerateSvgPages());
        }
    }

    [Fact]
    public void DoesNotRunTheCheckMerelyToBuildADocument()
    {
        // Construction, layout and mutation must stay free of licensing concerns:
        // only producing an artefact triggers the notice.
        using PdfDocument document = new();
        document.Add(new TextBlock("No output produced"));
    }

    private static PdfDocument OnePage()
    {
        PdfDocument document = new();
        document.Add(new TextBlock("Entry point"));
        return document;
    }
}
