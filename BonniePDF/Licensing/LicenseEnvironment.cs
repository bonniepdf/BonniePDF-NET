using System;
using System.Diagnostics;
using System.Reflection;

namespace BonniePDF;

/// <summary>
/// How the licence check classifies the runtime it is hosted in.
/// </summary>
internal enum LicenseEnvironmentKind
{
    /// <summary>No positive evidence either way. Never fatal.</summary>
    Unknown,

    /// <summary>Positive evidence of development, test, or CI.</summary>
    Development,

    /// <summary>Positive evidence of production.</summary>
    Production,
}

/// <summary>
/// The signals the classifier considers, as a plain record.
/// </summary>
/// <remarks>
/// Reading the ambient environment and deciding what it means are separate on
/// purpose. The decision is a pure function of this record, so the conformance suite
/// can assert the whole matrix without mutating process state — and the ambient
/// reader can be defensive without that defensiveness obscuring the rule it feeds.
/// </remarks>
internal readonly struct EnvironmentSignals : IEquatable<EnvironmentSignals>
{
    /// <summary>Creates a signal record.</summary>
    public EnvironmentSignals(string? hostEnvironment, string? ci, bool testRunner, bool debuggerAttached)
    {
        HostEnvironment = hostEnvironment;
        Ci = ci;
        TestRunner = testRunner;
        DebuggerAttached = debuggerAttached;
    }

    /// <summary><c>DOTNET_ENVIRONMENT</c>, or <c>ASPNETCORE_ENVIRONMENT</c> when that is unset.</summary>
    public string? HostEnvironment { get; }

    /// <summary>The <c>CI</c> environment variable.</summary>
    public string? Ci { get; }

    /// <summary>Whether a known test framework is loaded into this process.</summary>
    public bool TestRunner { get; }

    /// <summary>Whether a debugger is attached.</summary>
    public bool DebuggerAttached { get; }

    /// <inheritdoc/>
    public bool Equals(EnvironmentSignals other) =>
        string.Equals(HostEnvironment, other.HostEnvironment, StringComparison.Ordinal)
        && string.Equals(Ci, other.Ci, StringComparison.Ordinal)
        && TestRunner == other.TestRunner
        && DebuggerAttached == other.DebuggerAttached;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is EnvironmentSignals other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(HostEnvironment, Ci, TestRunner, DebuggerAttached);

    /// <summary>Equality operator.</summary>
    public static bool operator ==(EnvironmentSignals left, EnvironmentSignals right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(EnvironmentSignals left, EnvironmentSignals right) => !left.Equals(right);
}

/// <summary>
/// Runtime classification for the licence declaration check.
/// </summary>
/// <remarks>
/// The check throws only on <b>positive evidence</b> of a development, test, or CI
/// environment; everywhere else it warns. That asymmetry is the whole design: a
/// false-positive throw stops a live workload, which is unrecoverable damage, while
/// a missed throw costs one conversation. So "I don't know" resolves to
/// <see cref="LicenseEnvironmentKind.Unknown"/>, and that never throws.
/// </remarks>
internal static class LicenseEnvironment
{
    /// <summary>
    /// Assembly-name prefixes that identify a hosting test framework.
    /// </summary>
    /// <remarks>
    /// Test suites are the single most valuable place for the check to be fatal: a
    /// red suite gets the declaration added once, permanently, by the person who
    /// owns the integration.
    /// </remarks>
    private static readonly string[] TestFrameworkAssemblyPrefixes =
    {
        "xunit",
        "nunit.framework",
        "Microsoft.VisualStudio.TestPlatform",
        "Microsoft.TestPlatform",
        "testhost",
        "ReSharperTestRunner",
    };

    /// <summary>Reads an environment variable, treating empty as unset.</summary>
    /// <remarks>
    /// Guarded: a constrained host can deny environment access with a
    /// <see cref="System.Security.SecurityException"/>, and that must not be able to
    /// take document generation down with it.
    /// </remarks>
    public static string? ReadVariable(string name)
    {
        try
        {
            string? value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? null : value;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Collects the ambient signals. Never throws, whatever the host provides.</summary>
    public static EnvironmentSignals ReadSignals()
    {
        string? hostEnvironment = ReadVariable("DOTNET_ENVIRONMENT") ?? ReadVariable("ASPNETCORE_ENVIRONMENT");

        bool debuggerAttached;
        try
        {
            debuggerAttached = Debugger.IsAttached;
        }
        catch (Exception)
        {
            debuggerAttached = false;
        }

        return new EnvironmentSignals(hostEnvironment, ReadVariable("CI"), IsTestRunnerLoaded(), debuggerAttached);
    }

    /// <summary>
    /// The classification rule, as a pure function.
    /// </summary>
    /// <remarks>
    /// Development evidence outranks production evidence when both are present: a CI
    /// job that sets the host environment to Production to exercise a production
    /// build is still CI, and should fail loudly rather than warn into a log nobody
    /// reads.
    /// </remarks>
    public static LicenseEnvironmentKind Classify(EnvironmentSignals signals)
    {
        // ── Positive development / test / CI evidence ──
        if (string.Equals(signals.HostEnvironment, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(signals.HostEnvironment, "Test", StringComparison.OrdinalIgnoreCase))
        {
            return LicenseEnvironmentKind.Development;
        }

        // `CI` is set to "false" by pipelines that want to *disable* CI-specific
        // behaviour, so presence alone is not evidence.
        if (signals.Ci is not null
            && !string.Equals(signals.Ci, "false", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(signals.Ci, "0", StringComparison.Ordinal))
        {
            return LicenseEnvironmentKind.Development;
        }

        if (signals.TestRunner) return LicenseEnvironmentKind.Development;
        if (signals.DebuggerAttached) return LicenseEnvironmentKind.Development;

        // ── Positive production evidence ──
        if (string.Equals(signals.HostEnvironment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            return LicenseEnvironmentKind.Production;
        }

        // A console application with no host environment set lands here, and warns
        // rather than throwing. That is not a gap: such a process has an attached
        // terminal, so a warning on stderr is exactly as visible as a throw would
        // have been.
        return LicenseEnvironmentKind.Unknown;
    }

    /// <summary>Classifies the current runtime.</summary>
    public static LicenseEnvironmentKind Classify() => Classify(ReadSignals());

    private static bool IsTestRunnerLoaded()
    {
        try
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                string? name = assembly.GetName().Name;
                if (name is null) continue;

                foreach (string prefix in TestFrameworkAssemblyPrefixes)
                {
                    if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
        }
        catch (Exception)
        {
            // Reflection over the load context is best-effort. Failing to enumerate
            // assemblies means "no evidence", not "not a test run".
        }

        return false;
    }

    /// <summary>
    /// Writes the one-time notice.
    /// </summary>
    /// <remarks>
    /// Both sinks, deliberately. <see cref="Trace"/> is where a .NET library
    /// conventionally reports this and is what shows up in the Visual Studio output
    /// window, but a plain console application has no trace listener attached, so
    /// trace alone would make the notice invisible in exactly the setting where
    /// someone is most likely to be reading. Standard error covers that.
    /// </remarks>
    public static void WriteWarning(string message)
    {
        try
        {
            Trace.TraceWarning(message);
        }
        catch (Exception)
        {
            // A listener that throws is not a reason to fail generation.
        }

        try
        {
            Console.Error.WriteLine(message);
        }
        catch (Exception)
        {
            // A redirected or closed stderr is not a reason to fail generation.
        }
    }
}
