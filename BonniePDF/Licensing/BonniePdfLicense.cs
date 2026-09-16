using System;
using System.Threading;

namespace BonniePDF.Licensing;

/// <summary>
/// The application's BonniePDF licence declaration.
/// </summary>
/// <remarks>
/// <para>
/// This is the only global mutable state in the library, and it holds exactly one
/// thing. Everything else that configures output lives on the document,
/// deliberately, so that two documents generated in the same process cannot
/// influence each other. The licence declaration is global because it is a
/// statement about the application, not about any one document — and it is not a
/// settings bag, so nothing else belongs here.
/// </para>
/// <para>
/// Not to be confused with <see cref="Configuration.DocumentSettings"/>, reached
/// through <c>document.Settings</c>, which is what "settings" means in this library.
/// </para>
/// </remarks>
public static class BonniePdfLicense
{
    /// <summary>Sentinel for "no declaration", so the backing field can be read lock-free.</summary>
    private const int Unset = -1;

    private static int _tier = Unset;

    /// <summary>
    /// Which BonniePDF licence applies to this application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Set once at startup, before generating the first document:
    /// </para>
    /// <code>
    /// BonniePdfLicense.Tier = LicenseTier.Community;
    /// </code>
    /// <para>
    /// There is no licence key, no activation, and no telemetry — this is a
    /// self-declaration. <c>BONNIEPDF_LICENSE</c> in the environment is an equivalent
    /// way to state it where a startup hook is awkward; this property takes
    /// precedence over it.
    /// </para>
    /// <para>
    /// Assigning a value outside <see cref="LicenseTier"/> throws
    /// <see cref="InvalidLicenseTierException"/> immediately: that is a bad cast in
    /// your startup code, not a licensing state.
    /// </para>
    /// <para>
    /// Assignment after the first document has been generated is allowed but has no
    /// retroactive effect — the one-time notice has already been decided.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidLicenseTierException">
    /// The assigned value is not a defined <see cref="LicenseTier"/>.
    /// </exception>
    public static LicenseTier? Tier
    {
        get
        {
            int value = Volatile.Read(ref _tier);
            return value == Unset ? null : (LicenseTier)value;
        }
        set
        {
            if (value is null)
            {
                Volatile.Write(ref _tier, Unset);
                return;
            }

            LicenseTier tier = value.Value;
            if (!Enum.IsDefined(typeof(LicenseTier), tier))
            {
                throw new InvalidLicenseTierException(LicenseMessages.InvalidTier(tier), nameof(value));
            }

            Volatile.Write(ref _tier, (int)tier);
        }
    }
}
