namespace BonniePDF.Licensing;

/// <summary>
/// The BonniePDF licence tiers.
/// </summary>
/// <remarks>
/// Every tier receives the <b>complete feature set</b>. Tiers differ only in usage
/// rights, never in capability — nothing in the library branches on the declared
/// tier except the one-time notice in the licence check.
/// </remarks>
public enum LicenseTier
{
    /// <summary>
    /// Internal evaluation, development, testing, security review, proof of concept,
    /// and non-production staging. Not time-limited, and fully functional — but not
    /// permitted for production use. Declaring it produces a one-time warning.
    /// <a href="https://www.bonniepdf.dev/legal">Learn more</a>
    /// </summary>
    Evaluation,

    /// <summary>
    /// Free under the BonniePDF Community License: qualifying personal, charity,
    /// education, research and open-source use, and commercial production use while
    /// the benefiting organisation's annual gross revenue is below US $1,000,000
    /// (aggregated with its affiliates).
    /// <a href="https://www.bonniepdf.dev/legal">Learn more</a>
    /// </summary>
    Community,

    /// <summary>
    /// Paid: one named legal entity, unlimited developers.
    /// <a href="https://www.bonniepdf.dev/pricing">Learn more</a>
    /// </summary>
    Professional,

    /// <summary>
    /// Paid: a named legal entity and the affiliates covered by its Order.
    /// <a href="https://www.bonniepdf.dev/pricing">Learn more</a>
    /// </summary>
    Enterprise,
}
