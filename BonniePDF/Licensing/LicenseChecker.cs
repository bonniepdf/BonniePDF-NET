using System;
using System.Threading;

namespace BonniePDF.Licensing;

/// <summary>
/// The licence declaration check.
/// </summary>
/// <remarks>
/// <para>
/// This is a <b>declaration, not an enforcement mechanism</b>. There is no licence
/// key, no activation, no network call, no telemetry, and no tier-dependent feature
/// gating anywhere in this library. The check exists to put the licence terms in
/// front of the developer integrating BonniePDF, once, at the moment they integrate
/// it.
/// </para>
/// <para>
/// Three invariants keep it honest, and all three are asserted by the shared
/// conformance fixture <c>licence-declaration/tier-invariance</c>:
/// </para>
/// <list type="number">
///   <item><description>
///     <b>It never changes output.</b> No watermark, no metadata field, no free-tier
///     degradation. The fixture generates the same document under every tier and
///     compares bytes.
///   </description></item>
///   <item><description>
///     <b>It fires at most once per process</b>, however many documents are generated.
///   </description></item>
///   <item><description>
///     <b>It never throws without positive evidence</b> of a development, test, or CI
///     environment. A production workload that never declared a tier warns and keeps
///     running.
///   </description></item>
/// </list>
/// </remarks>
public static class LicenseChecker
{
    /// <summary>Whether the one-time notice has already been decided for this process.</summary>
    private static int _checked;

    /// <summary>Resolves <c>BONNIEPDF_LICENSE</c>, case-insensitively.</summary>
    /// <remarks>
    /// An unrecognised value is ignored rather than thrown: an operational
    /// environment variable must never be able to break document generation.
    /// </remarks>
    private static LicenseTier? EnvironmentDeclaration()
    {
        string? raw = LicenseEnvironment.ReadVariable("BONNIEPDF_LICENSE")?.Trim();
        if (raw is null) return null;

        return Enum.TryParse(raw, ignoreCase: true, out LicenseTier tier)
               && Enum.IsDefined(typeof(LicenseTier), tier)
            ? tier
            : null;
    }

    /// <summary>How the caller wants an undeclared licence reported.</summary>
    private enum Enforcement
    {
        /// <summary>No override; the environment classification decides.</summary>
        Default,
        Throw,
        Warn,
    }

    /// <summary>Resolves <c>BONNIEPDF_LICENSE_ENFORCEMENT</c>.</summary>
    /// <remarks>
    /// Note the absence of a "silent" value, and that anything unrecognised falls
    /// through to environment detection. The consumer chooses whether the notice is
    /// fatal; they cannot switch it off. Declaring a tier is the supported way to
    /// silence it, and takes one line.
    /// </remarks>
    private static Enforcement EnforcementOverride()
    {
        string? raw = LicenseEnvironment.ReadVariable("BONNIEPDF_LICENSE_ENFORCEMENT")?.Trim();
        if (raw is null) return Enforcement.Default;
        if (string.Equals(raw, "throw", StringComparison.OrdinalIgnoreCase)) return Enforcement.Throw;
        if (string.Equals(raw, "warn", StringComparison.OrdinalIgnoreCase)) return Enforcement.Warn;
        return Enforcement.Default;
    }

    /// <summary>
    /// The effective declaration: the in-code property first, the environment
    /// variable second.
    /// </summary>
    public static LicenseTier? EffectiveTier() => BonniePdfLicense.Tier ?? EnvironmentDeclaration();

    /// <summary>
    /// Runs the licence check if it has not already run in this process.
    /// </summary>
    /// <remarks>
    /// Called at the top of every public generation entry point. Cheap after the
    /// first call — a single volatile read — so it costs nothing on the hot path of
    /// a batch job.
    /// </remarks>
    /// <exception cref="BonniePdfLicenseException">
    /// No tier is declared and the environment is positively identified as
    /// development, test, or CI.
    /// </exception>
    public static void ValidateLicense() => ValidateLicense(LicenseEnvironment.Classify());

    /// <summary>
    /// The check, against a caller-supplied classification.
    /// </summary>
    /// <remarks>
    /// Internal seam for the conformance suite. The suite cannot exercise the
    /// production and unknown rows of the outcome matrix through the public entry
    /// point, because a loaded test framework is itself one of the signals the
    /// classifier reads — a test process is always classified as development. So the
    /// suite supplies the classification and asserts the rule, while
    /// <see cref="LicenseEnvironment.Classify(EnvironmentSignals)"/> is asserted
    /// separately as a pure function.
    /// </remarks>
    internal static void ValidateLicense(LicenseEnvironmentKind environment)
    {
        if (Volatile.Read(ref _checked) != 0) return;
        Volatile.Write(ref _checked, 1);

        LicenseTier? tier = EffectiveTier();

        if (tier == LicenseTier.Evaluation)
        {
            LicenseEnvironment.WriteWarning(LicenseMessages.Evaluation);
            return;
        }

        // Community / Professional / Enterprise: silent.
        if (tier is not null) return;

        Enforcement enforcement = EnforcementOverride();
        bool fatal = enforcement == Enforcement.Throw
                     || (enforcement != Enforcement.Warn
                         && environment == LicenseEnvironmentKind.Development);

        if (fatal)
        {
            // Reset so the throw is not "spent": the next generation attempt should
            // throw again, or the consumer would see it once, ignore it, and never
            // be reminded. Memoisation exists to stop log spam from warnings, not to
            // make a fatal error a one-off.
            Volatile.Write(ref _checked, 0);
            throw new BonniePdfLicenseException(LicenseMessages.Undeclared);
        }

        LicenseEnvironment.WriteWarning(LicenseMessages.Undeclared);
    }

    /// <summary>
    /// Clears the once-per-process memo.
    /// </summary>
    /// <remarks>
    /// Exported for the conformance suite, which asserts the outcome matrix row by
    /// row and needs each row to start from a clean state. Calling it in application
    /// code does nothing useful — it can only cause the notice to be shown again.
    /// </remarks>
    public static void ResetForTesting() => Volatile.Write(ref _checked, 0);
}
