namespace BonniePDF.Validation.Services;

/// <summary>
/// Default implementation of PDF directory provider
/// </summary>
public class DefaultPdfDirectoryProvider
{
    private readonly string? _directory;

    /// <summary>
    /// Initializes a new instance with a specific directory
    /// </summary>
    public DefaultPdfDirectoryProvider(string? directory)
    {
        _directory = directory;
    }

    /// <summary>
    /// Initializes a new instance with the default directory
    /// </summary>
    public static DefaultPdfDirectoryProvider CreateDefault()
    {
        string defaultPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", "..", "BonniePDF.Examples", "bin", "Debug", "net8.0", "GeneratedPdfs"
        );
        
        // Try to resolve the absolute path
        defaultPath = System.IO.Path.GetFullPath(defaultPath);
        
        return new DefaultPdfDirectoryProvider(defaultPath);
    }

    /// <summary>
    /// Gets the directory path
    /// </summary>
    public string? GetDirectory()
    {
        return _directory;
    }
}
