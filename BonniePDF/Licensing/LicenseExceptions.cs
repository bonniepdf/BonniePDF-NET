using System;

namespace BonniePDF.Licensing;

/// <summary>
/// Thrown when the library is asked to generate a document without a licence
/// declaration, in an environment positively identified as development, test, or CI.
/// </summary>
/// <remarks>
/// <para>
/// It is never thrown in production or in an unclassifiable environment — there the
/// notice degrades to a one-time warning, so that a live workload which happens
/// never to have declared a tier keeps running.
/// </para>
/// <para>
/// Deliberately <b>not</b> derived from <see cref="Core.BonniePdfException"/>. That
/// type means "BonniePDF rejected this input or operation", and applications catch
/// it to handle malformed content as one category. A missing licence declaration is
/// neither malformed content nor a recoverable authoring error, and an existing
/// content-error handler swallowing it would defeat the only purpose this exception
/// has: being noticed.
/// </para>
/// </remarks>
public sealed class BonniePdfLicenseException : Exception
{
    /// <summary>Creates a new <see cref="BonniePdfLicenseException"/>.</summary>
    public BonniePdfLicenseException(string message) : base(message)
    {
        HelpLink = "https://www.bonniepdf.dev/legal";
    }

    /// <summary>Creates a new <see cref="BonniePdfLicenseException"/> wrapping an inner exception.</summary>
    public BonniePdfLicenseException(string message, Exception innerException)
        : base(message, innerException)
    {
        HelpLink = "https://www.bonniepdf.dev/legal";
    }

    /// <summary>Creates a new <see cref="BonniePdfLicenseException"/> with no message.</summary>
    public BonniePdfLicenseException()
    {
        HelpLink = "https://www.bonniepdf.dev/legal";
    }
}

/// <summary>
/// Thrown when <see cref="BonniePdfLicense.Tier"/> is assigned a value outside
/// the <see cref="LicenseTier"/> enumeration — a typo or a bad cast in the
/// consumer's startup code, not an undeclared licence.
/// </summary>
/// <remarks>
/// Distinct from <see cref="BonniePdfLicenseException"/> on purpose: this one throws
/// in every environment, immediately, at the assignment that caused it, because it
/// is a programming error rather than a licensing state.
/// </remarks>
public sealed class InvalidLicenseTierException : ArgumentException
{
    /// <summary>Creates a new <see cref="InvalidLicenseTierException"/>.</summary>
    public InvalidLicenseTierException(string message, string paramName)
        : base(message, paramName)
    {
    }

    /// <summary>Creates a new <see cref="InvalidLicenseTierException"/>.</summary>
    public InvalidLicenseTierException(string message) : base(message)
    {
    }

    /// <summary>Creates a new <see cref="InvalidLicenseTierException"/>.</summary>
    public InvalidLicenseTierException()
    {
    }

    /// <summary>Creates a new <see cref="InvalidLicenseTierException"/> wrapping an inner exception.</summary>
    public InvalidLicenseTierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
