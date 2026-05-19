namespace BonniePDF.Validation.Models;

/// <summary>
/// Immutable data model for PDF validation results
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets whether the validation request completed successfully
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets whether the PDF is compliant with the PDF/A standard
    /// </summary>
    public bool IsCompliant { get; }

    /// <summary>
    /// Gets the compliance level achieved (e.g., "PDF/A-1b")
    /// </summary>
    public string? ComplianceLevel { get; }

    /// <summary>
    /// Gets the error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the list of validation errors (if not compliant)
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Gets the raw response from the validation API
    /// </summary>
    public string? RawResponse { get; }

    /// <summary>
    /// Gets the PDF file name that was validated
    /// </summary>
    public string PdfFileName { get; }

    /// <summary>
    /// Gets the timestamp when the validation was performed
    /// </summary>
    public DateTime ValidatedAt { get; }

    /// <summary>
    /// Gets the full file path of the PDF
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Gets the file size in bytes
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// Gets the duration of the validation process
    /// </summary>
    public TimeSpan ValidationDuration { get; }

    /// <summary>
    /// Initializes a new instance of the ValidationResult class
    /// </summary>
    public ValidationResult(
        bool isSuccess,
        bool isCompliant,
        string? complianceLevel,
        string? errorMessage,
        IReadOnlyList<string> errors,
        string? rawResponse,
        string pdfFileName,
        string filePath,
        long fileSize,
        TimeSpan validationDuration)
    {
        IsSuccess = isSuccess;
        IsCompliant = isCompliant;
        ComplianceLevel = complianceLevel;
        ErrorMessage = errorMessage;
        Errors = errors ?? Array.Empty<string>();
        RawResponse = rawResponse;
        PdfFileName = pdfFileName ?? throw new ArgumentNullException(nameof(pdfFileName));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FileSize = fileSize;
        ValidationDuration = validationDuration;
        ValidatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult CreateSuccess(
        string pdfFileName,
        string filePath,
        long fileSize,
        TimeSpan validationDuration,
        bool isCompliant,
        string? complianceLevel,
        IReadOnlyList<string> errors,
        string? rawResponse)
    {
        return new ValidationResult(
            isSuccess: true,
            isCompliant: isCompliant,
            complianceLevel: complianceLevel,
            errorMessage: null,
            errors: errors,
            rawResponse: rawResponse,
            pdfFileName: pdfFileName,
            filePath: filePath,
            fileSize: fileSize,
            validationDuration: validationDuration
        );
    }

    /// <summary>
    /// Creates a failed validation result
    /// </summary>
    public static ValidationResult CreateFailure(
        string pdfFileName,
        string filePath,
        long fileSize,
        TimeSpan validationDuration,
        string errorMessage,
        string? rawResponse)
    {
        return new ValidationResult(
            isSuccess: false,
            isCompliant: false,
            complianceLevel: null,
            errorMessage: errorMessage,
            errors: Array.Empty<string>(),
            rawResponse: rawResponse,
            pdfFileName: pdfFileName,
            filePath: filePath,
            fileSize: fileSize,
            validationDuration: validationDuration
        );
    }
}
