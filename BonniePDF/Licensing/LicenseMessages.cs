namespace BonniePDF;

/// <summary>
/// The licence notice copy.
/// </summary>
/// <remarks>
/// The wording is not byte-parity-constrained — it is prose, and each implementation
/// writes it in its own idiom — but the points the shared specification enumerates
/// are binding, and the conformance fixture asserts several of them by substring.
/// Read the specification before editing this file.
/// <para>
/// These strings are the only place in the library that talks to the user about
/// money. Keep them short, factual, and free of pressure: the tone is a colleague
/// telling you which box to tick, not a nag screen.
/// </para>
/// </remarks>
internal static class LicenseMessages
{
    internal const string PricingUrl = "https://www.bonniepdf.dev/pricing";
    internal const string LicenceUrl = "https://www.bonniepdf.dev/legal";

    /// <summary>Prefix on every notice, so they stay greppable in aggregated logs.</summary>
    internal const string NoticePrefix = "[BonniePDF]";

    /// <summary>
    /// Shown once when generation is attempted with no declaration. Thrown in a
    /// development/test/CI environment, warned everywhere else — the same text
    /// either way, because the reader needs the same information in both cases.
    /// </summary>
    internal static readonly string Undeclared = $"""
        {NoticePrefix} Please declare which BonniePDF licence applies to you, by
        setting 'BonniePdfLicense.Tier' once at application startup.

        Welcome to BonniePDF, and thank you for using it.

        It is one line, before you generate your first document:


            BonniePdfLicense.Tier = LicenseTier.Community;
            // or Evaluation / Professional / Enterprise

        Or set BONNIEPDF_LICENSE=community in the environment, which is often easier
        in a container, a build agent, or CI.

        The tiers:

          Evaluation   - Start here. Evaluate the library for as long as you need:
                         development, testing, security review, proof of concept,
                         and non-production staging. Not permitted for production
                         use.
          Community    - Free. Individuals, non-profits, charities, education,
                         research, and open-source projects; and commercial
                         production use while the Benefiting Organisation has annual
                         gross revenue below US $1,000,000.
          Professional - Paid. One named legal entity, unlimited developers.
          Enterprise   - Paid. A named legal entity and the affiliates covered by
                         your Order.

        The US $1,000,000 threshold is measured on the Benefiting Organisation - the
        organisation whose documents these are - together with its affiliates. If you
        are producing documents for a client, that is the client, not you. Crossing
        the threshold allows ninety days to obtain a paid licence.

        Every tier includes the complete feature set. Tiers differ only in usage
        rights, never in capability.

        There is no licence key, no activation, and no telemetry. This setting is a
        self-declaration and nothing is sent anywhere; we simply trust you. Choosing
        the tier that matches your situation is what keeps BonniePDF maintained.

        Evaluating at work? Start now with the Evaluation tier - that is exactly what
        it is for, and the purchasing decision does not have to be yours. Forward the
        pricing page to whoever makes it when your team is ready.

        Pricing:  {PricingUrl}
        Licences: {LicenceUrl}

        """;

    /// <summary>
    /// Shown once when <see cref="LicenseTier.Evaluation"/> is declared. Warned in
    /// every environment, including production, because the evaluation tier is
    /// precisely the one that is <i>not</i> permitted there — and someone has to say
    /// so where an operator might see it.
    /// </summary>
    internal static readonly string Evaluation = $"""
        {NoticePrefix} Running under the Evaluation licence.
        This tier is fully functional and intended for evaluating BonniePDF:
        development, testing, security review, proof of concept, and non-production
        staging. It is not permitted for production use.

        When you are ready, set 'BonniePdfLicense.Tier' to the tier that applies
        to you. It is free for organisations below US $1,000,000 annual gross
        revenue.

        Pricing:  {PricingUrl}
        Licences: {LicenceUrl}
        """;

    /// <summary>Shown when <c>License</c> is assigned a value outside the enumeration.</summary>
    internal static string InvalidTier(LicenseTier value) =>
        $"{NoticePrefix} '{(int)value}' is not a BonniePDF licence tier. Expected one of: "
        + "Evaluation, Community, Professional, Enterprise."
        + System.Environment.NewLine
        + System.Environment.NewLine
        + "Licences: " + LicenceUrl;
}
