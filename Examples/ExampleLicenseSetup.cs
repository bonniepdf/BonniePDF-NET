using System.Runtime.CompilerServices;
using BonniePDF;

namespace BonniePDF.Examples.Shared;

/// <summary>
/// Declares a licence tier for every example project.
/// </summary>
/// <remarks>
/// <para>
/// BonniePDF asks the consuming application to declare which licence applies to it,
/// once at startup. The examples are non-production demonstrations, so
/// <see cref="LicenseTier.Evaluation"/> is the honest declaration for them.
/// </para>
/// <para>
/// Note what this file is <b>not</b> saying: it does not declare a tier on your
/// behalf. If you are copying an example into your own project, replace this with
/// the tier that applies to you — <see cref="LicenseTier.Community"/> is free for
/// organisations below US $1,000,000 annual gross revenue. See
/// <a href="https://www.bonniepdf.dev/legal">the licensing overview</a>.
/// </para>
/// <para>
/// Linked into every example project by <c>Examples/Directory.Build.props</c> rather
/// than repeated in 58 <c>Program.cs</c> files. A module initializer, so it runs
/// before <c>Main</c> whatever the example's entry point looks like.
/// </para>
/// </remarks>
internal static class ExampleLicenseSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        BonniePdfLicense.Tier = LicenseTier.Evaluation;
    }
}
