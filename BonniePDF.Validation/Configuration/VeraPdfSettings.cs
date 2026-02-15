namespace BonniePDF.Validation.Configuration;

/// <summary>
/// Configuration settings for VeraPDF API connection
/// </summary>
public class VeraPdfSettings
{
    /// <summary>
    /// Gets the base URL of the VeraPDF API
    /// </summary>
    public string BaseUrl { get; init; } = "http://localhost:8080/api";

    /// <summary>
    /// Gets the timeout for API requests
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets the maximum number of retry attempts for transient failures
    /// </summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>
    /// Gets the delay between retry attempts
    /// </summary>
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Creates a new instance with default settings
    /// </summary>
    public static VeraPdfSettings Default => new();
}
