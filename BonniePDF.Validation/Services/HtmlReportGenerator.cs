using System.Text;
using System.Text.Json;
using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.Services;

/// <summary>
/// Generates detailed HTML validation reports with interactive features
/// </summary>
public class HtmlReportGenerator : IReportGenerator
{
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the HtmlReportGenerator
    /// </summary>
    public HtmlReportGenerator()
    {
    }

    /// <summary>
    /// Generates a validation report
    /// </summary>
    public async Task GenerateAsync(
        IEnumerable<ValidationResult> results,
        string validationProfile,
        string sourceDirectory,
        string outputPath)
    {
        var resultsList = results.ToList();
        var summary = ValidationSummary.FromResults(resultsList);
        
        var html = new StringBuilder();
        
        // HTML Header
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine("    <title>BonniePDF Validation Report</title>");
        html.AppendLine(GetCssStyles());
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");
        
        // Header Section
        html.AppendLine(GetHeaderSection(validationProfile, sourceDirectory));
        
        // Summary Section
        html.AppendLine(GetSummarySection(summary));
        
        // Controls Section
        html.AppendLine(GetControlsSection());
        
        // Results Section
        html.AppendLine(GetResultsSection(resultsList));
        
        // Footer Section
        html.AppendLine(GetFooterSection(summary, validationProfile));
        
        html.AppendLine("    </div>");
        
        // JavaScript
        html.AppendLine("<script>");
        html.AppendLine(GetJavaScriptData(resultsList, summary, validationProfile, sourceDirectory));
        html.AppendLine(GetJavaScriptLogic());
        html.AppendLine("</script>");
        
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        
        await System.IO.File.WriteAllTextAsync(outputPath, html.ToString());
    }

    private static string GetCssStyles()
    {
        return @"
    <style>
        :root {
            --color-primary: #2563eb;
            --color-primary-dark: #1e40af;
            --color-success: #10b981;
            --color-success-dark: #059669;
            --color-danger: #ef4444;
            --color-danger-dark: #dc2626;
            --color-warning: #f59e0b;
            --color-warning-dark: #d97706;
            --color-neutral: #6b7280;
            --color-bg: #f9fafb;
            --color-surface: #ffffff;
            --color-border: #e5e7eb;
            --color-text: #111827;
            --color-text-secondary: #6b7280;
            --shadow-sm: 0 1px 2px 0 rgb(0 0 0 / 0.05);
            --shadow-md: 0 4px 6px -1px rgb(0 0 0 / 0.1);
            --shadow-lg: 0 10px 15px -3px rgb(0 0 0 / 0.1);
            --radius-sm: 0.25rem;
            --radius-md: 0.5rem;
            --radius-lg: 0.75rem;
        }

        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
            background-color: var(--color-bg);
            color: var(--color-text);
            line-height: 1.6;
            font-size: 16px;
        }

        .container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 2rem;
        }

        /* Header Styles */
        .report-header {
            background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-primary-dark) 100%);
            color: white;
            padding: 2rem;
            border-radius: var(--radius-lg);
            margin-bottom: 2rem;
            box-shadow: var(--shadow-lg);
        }

        .header-content {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            flex-wrap: wrap;
            gap: 1rem;
        }

        .logo-section h1 {
            font-size: 2rem;
            font-weight: 700;
            margin-bottom: 0.5rem;
        }

        .subtitle {
            font-size: 1rem;
            opacity: 0.9;
        }

        .metadata {
            display: flex;
            flex-direction: column;
            gap: 0.5rem;
            font-size: 0.875rem;
        }

        .metadata-item {
            display: flex;
            gap: 0.5rem;
        }

        .metadata-item .label {
            opacity: 0.8;
        }

        /* Summary Cards */
        .summary-section {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 1.5rem;
            margin-bottom: 2rem;
        }

        .stat-card {
            background: var(--color-surface);
            padding: 1.5rem;
            border-radius: var(--radius-lg);
            box-shadow: var(--shadow-md);
            display: flex;
            align-items: center;
            gap: 1rem;
            transition: transform 0.2s, box-shadow 0.2s;
        }

        .stat-card:hover {
            transform: translateY(-2px);
            box-shadow: var(--shadow-lg);
        }

        .stat-card.total {
            border-left: 4px solid var(--color-primary);
        }

        .stat-card.passed {
            border-left: 4px solid var(--color-success);
        }

        .stat-card.failed {
            border-left: 4px solid var(--color-danger);
        }

        .stat-card.errors {
            border-left: 4px solid var(--color-warning);
        }

        .stat-icon {
            font-size: 2rem;
            width: 3rem;
            height: 3rem;
            display: flex;
            align-items: center;
            justify-content: center;
            background: var(--color-bg);
            border-radius: var(--radius-md);
        }

        .stat-content {
            flex: 1;
        }

        .stat-value {
            font-size: 2rem;
            font-weight: 700;
            line-height: 1;
        }

        .stat-label {
            font-size: 0.875rem;
            color: var(--color-text-secondary);
            margin-top: 0.25rem;
        }

        .stat-percentage {
            font-size: 1.25rem;
            font-weight: 600;
            color: var(--color-success);
        }

        /* Controls Section */
        .controls-section {
            background: var(--color-surface);
            padding: 1.5rem;
            border-radius: var(--radius-lg);
            box-shadow: var(--shadow-md);
            margin-bottom: 2rem;
            display: flex;
            flex-wrap: wrap;
            gap: 1rem;
            align-items: center;
        }

        .search-container {
            flex: 1;
            min-width: 300px;
            display: flex;
            gap: 0.5rem;
        }

        #search-input {
            flex: 1;
            padding: 0.75rem 1rem;
            border: 2px solid var(--color-border);
            border-radius: var(--radius-md);
            font-size: 1rem;
            transition: border-color 0.2s;
        }

        #search-input:focus {
            outline: none;
            border-color: var(--color-primary);
        }

        .filter-container {
            display: flex;
            gap: 0.5rem;
        }

        .filter-btn {
            padding: 0.75rem 1.5rem;
            border: 2px solid var(--color-border);
            background: var(--color-surface);
            border-radius: var(--radius-md);
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        }

        .filter-btn:hover {
            border-color: var(--color-primary);
            background: var(--color-bg);
        }

        .filter-btn.active {
            background: var(--color-primary);
            color: white;
            border-color: var(--color-primary);
        }

        .actions-container {
            display: flex;
            gap: 0.5rem;
        }

        .btn-secondary {
            padding: 0.75rem 1.5rem;
            border: 2px solid var(--color-border);
            background: var(--color-surface);
            border-radius: var(--radius-md);
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        }

        .btn-secondary:hover {
            border-color: var(--color-primary);
            background: var(--color-bg);
        }

        /* Results Section */
        .results-section h2 {
            font-size: 1.5rem;
            margin-bottom: 1.5rem;
            color: var(--color-text);
        }

        #results-container {
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        .result-card {
            background: var(--color-surface);
            border-radius: var(--radius-lg);
            box-shadow: var(--shadow-md);
            overflow: hidden;
            transition: all 0.3s ease;
        }

        .result-card.hidden {
            display: none;
        }

        .card-header {
            padding: 1.5rem;
            cursor: pointer;
            display: flex;
            justify-content: space-between;
            align-items: center;
            transition: background-color 0.2s;
        }

        .card-header:hover {
            background-color: var(--color-bg);
        }

        .file-info {
            display: flex;
            align-items: center;
            gap: 1rem;
            flex: 1;
        }

        .status-badge {
            padding: 0.5rem 1rem;
            border-radius: var(--radius-md);
            font-weight: 700;
            font-size: 0.875rem;
            white-space: nowrap;
        }

        .status-badge.passed {
            background: #d1fae5;
            color: var(--color-success-dark);
        }

        .status-badge.failed {
            background: #fee2e2;
            color: var(--color-danger-dark);
        }

        .status-badge.error {
            background: #fef3c7;
            color: var(--color-warning-dark);
        }

        .file-name {
            font-size: 1.125rem;
            font-weight: 600;
            margin: 0;
        }

        .file-size {
            color: var(--color-text-secondary);
            font-size: 0.875rem;
        }

        .card-actions {
            display: flex;
            align-items: center;
        }

        .chevron {
            font-size: 1rem;
            transition: transform 0.3s ease;
        }

        .result-card.expanded .chevron {
            transform: rotate(180deg);
        }

        .card-content {
            max-height: 0;
            overflow: hidden;
            transition: max-height 0.3s ease;
            border-top: 1px solid var(--color-border);
        }

        .result-card.expanded .card-content {
            max-height: 2000px;
        }

        .validation-details {
            padding: 1.5rem;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 1rem;
        }

        .detail-row {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        .detail-label {
            font-size: 0.875rem;
            color: var(--color-text-secondary);
            font-weight: 600;
        }

        .detail-value {
            font-size: 1rem;
            color: var(--color-text);
        }

        .error-message {
            padding: 1rem 1.5rem;
            background: #fef3c7;
            border-left: 4px solid var(--color-warning);
            margin: 0 1.5rem 1rem 1.5rem;
            border-radius: var(--radius-md);
        }

        .errors-list {
            padding: 0 1.5rem 1.5rem 1.5rem;
        }

        .errors-list h4 {
            font-size: 1rem;
            margin-bottom: 1rem;
            color: var(--color-danger);
        }

        .error-items {
            list-style: none;
            display: flex;
            flex-direction: column;
            gap: 1rem;
        }

        .error-item {
            background: var(--color-bg);
            padding: 1rem;
            border-radius: var(--radius-md);
            border-left: 4px solid var(--color-danger);
        }

        .error-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 0.5rem;
            flex-wrap: wrap;
            gap: 0.5rem;
        }

        .error-spec {
            font-weight: 600;
            color: var(--color-danger-dark);
        }

        .error-description {
            color: var(--color-text);
            margin-bottom: 0.5rem;
        }

        .error-location {
            color: var(--color-text-secondary);
            font-size: 0.875rem;
            font-style: italic;
        }

        /* Footer */
        .report-footer {
            margin-top: 3rem;
            padding: 2rem;
            text-align: center;
            color: var(--color-text-secondary);
            border-top: 1px solid var(--color-border);
        }

        .footer-content p {
            margin: 0.25rem 0;
        }

        /* Responsive Design */
        @media (max-width: 768px) {
            .container {
                padding: 1rem;
            }

            .header-content {
                flex-direction: column;
            }

            .summary-section {
                grid-template-columns: 1fr;
            }

            .controls-section {
                flex-direction: column;
                align-items: stretch;
            }

            .filter-container {
                flex-wrap: wrap;
            }

            .file-info {
                flex-direction: column;
                align-items: flex-start;
            }

            .validation-details {
                grid-template-columns: 1fr;
            }
        }

        @media print {
            body {
                background: white;
            }

            .controls-section {
                display: none;
            }

            .result-card {
                break-inside: avoid;
            }

            .card-content {
                max-height: none !important;
            }
        }
    </style>";
    }

    private static string GetHeaderSection(string validationProfile, string sourceDirectory)
    {
        return $@"
        <header class=""report-header"">
            <div class=""header-content"">
                <div class=""logo-section"">
                    <h1>BonniePDF Validation Report</h1>
                    <p class=""subtitle"">PDF/A Compliance Validation Results</p>
                </div>
                <div class=""metadata"">
                    <div class=""metadata-item"">
                        <span class=""label"">Generated:</span>
                        <span class=""value"">{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</span>
                    </div>
                    <div class=""metadata-item"">
                        <span class=""label"">Profile:</span>
                        <span class=""value"">PDF/A-{validationProfile.ToUpper()}</span>
                    </div>
                    <div class=""metadata-item"">
                        <span class=""label"">Source:</span>
                        <span class=""value"">{System.Net.WebUtility.HtmlEncode(sourceDirectory)}</span>
                    </div>
                </div>
            </div>
        </header>";
    }

    private static string GetSummarySection(ValidationSummary summary)
    {
        return $@"
        <section class=""summary-section"">
            <div class=""stat-card total"">
                <div class=""stat-icon"">📄</div>
                <div class=""stat-content"">
                    <div class=""stat-value"">{summary.TotalPdfs}</div>
                    <div class=""stat-label"">Total PDFs</div>
                </div>
            </div>
            <div class=""stat-card passed"">
                <div class=""stat-icon"">✓</div>
                <div class=""stat-content"">
                    <div class=""stat-value"">{summary.PassedCount}</div>
                    <div class=""stat-label"">Passed</div>
                </div>
                <div class=""stat-percentage"">{summary.PassRate:F1}%</div>
            </div>
            <div class=""stat-card failed"">
                <div class=""stat-icon"">✗</div>
                <div class=""stat-content"">
                    <div class=""stat-value"">{summary.FailedCount}</div>
                    <div class=""stat-label"">Failed</div>
                </div>
            </div>
            <div class=""stat-card errors"">
                <div class=""stat-icon"">⚠</div>
                <div class=""stat-content"">
                    <div class=""stat-value"">{summary.ErrorCount}</div>
                    <div class=""stat-label"">Errors</div>
                </div>
            </div>
        </section>";
    }

    private static string GetControlsSection()
    {
        return @"
        <section class=""controls-section"">
            <div class=""search-container"">
                <input type=""text"" id=""search-input"" placeholder=""Search PDFs or errors..."" />
                <button id=""clear-search"" class=""btn-secondary"" style=""display:none;"">Clear</button>
            </div>
            <div class=""filter-container"">
                <button class=""filter-btn active"" data-filter=""all"">All</button>
                <button class=""filter-btn"" data-filter=""passed"">Passed</button>
                <button class=""filter-btn"" data-filter=""failed"">Failed</button>
                <button class=""filter-btn"" data-filter=""error"">Errors</button>
            </div>
            <div class=""actions-container"">
                <button id=""expand-all"" class=""btn-secondary"">Expand All</button>
                <button id=""collapse-all"" class=""btn-secondary"">Collapse All</button>
            </div>
        </section>";
    }

    private static string GetResultsSection(List<ValidationResult> results)
    {
        var sb = new StringBuilder();
        sb.AppendLine(@"        <section class=""results-section"">");
        sb.AppendLine(@"            <h2>Validation Results</h2>");
        sb.AppendLine(@"            <div id=""results-container"">");

        foreach (var result in results)
        {
            sb.AppendLine(GetResultCard(result));
        }

        sb.AppendLine(@"            </div>");
        sb.AppendLine(@"        </section>");
        return sb.ToString();
    }

    private static string GetResultCard(ValidationResult result)
    {
        string statusClass = result.IsSuccess
            ? (result.IsCompliant ? "passed" : "failed")
            : "error";

        string statusText = result.IsSuccess
            ? (result.IsCompliant ? "PASSED" : "FAILED")
            : "ERROR";

        string statusIcon = result.IsSuccess
            ? (result.IsCompliant ? "✓" : "✗")
            : "⚠";

        var sb = new StringBuilder();
        
        sb.AppendLine($@"                <article class=""result-card {statusClass}"" data-status=""{statusClass}"" data-filename=""{result.PdfFileName.ToLower()}"">");
        sb.AppendLine(@"                    <div class=""card-header"" onclick=""toggleCard(this)"">");
        sb.AppendLine(@"                        <div class=""file-info"">");
        sb.AppendLine($@"                            <span class=""status-badge {statusClass}"">{statusIcon} {statusText}</span>");
        sb.AppendLine($@"                            <h3 class=""file-name"">{System.Net.WebUtility.HtmlEncode(result.PdfFileName)}</h3>");
        sb.AppendLine($@"                            <span class=""file-size"">{FormatFileSize(result.FileSize)}</span>");
        sb.AppendLine(@"                        </div>");
        sb.AppendLine(@"                        <div class=""card-actions"">");
        sb.AppendLine(@"                            <span class=""chevron"">▼</span>");
        sb.AppendLine(@"                        </div>");
        sb.AppendLine(@"                    </div>");
        sb.AppendLine(@"                    <div class=""card-content"">");
        sb.AppendLine(@"                        <div class=""validation-details"">");
        sb.AppendLine($@"                            <div class=""detail-row"">
                                <span class=""detail-label"">Compliance Level:</span>
                                <span class=""detail-value"">{result.ComplianceLevel ?? "N/A"}</span>
                            </div>");
        sb.AppendLine($@"                            <div class=""detail-row"">
                                <span class=""detail-label"">Validation Time:</span>
                                <span class=""detail-value"">{result.ValidatedAt:yyyy-MM-dd HH:mm:ss}</span>
                            </div>");
        sb.AppendLine($@"                            <div class=""detail-row"">
                                <span class=""detail-label"">Duration:</span>
                                <span class=""detail-value"">{result.ValidationDuration.TotalSeconds:F2}s</span>
                            </div>");
        sb.AppendLine(@"                        </div>");

        if (!result.IsSuccess && !string.IsNullOrEmpty(result.ErrorMessage))
        {
            sb.AppendLine($@"                        <div class=""error-message"">
                            <strong>Error:</strong> {System.Net.WebUtility.HtmlEncode(result.ErrorMessage)}
                        </div>");
        }

        if (!result.IsCompliant && result.Errors.Count > 0)
        {
            sb.AppendLine(@"                        <div class=""errors-list"">");
            sb.AppendLine(@"                            <h4>Validation Errors</h4>");
            sb.AppendLine(@"                            <ul class=""error-items"">");

            foreach (var error in result.Errors)
            {
                sb.AppendLine($@"                                <li class=""error-item"">
                                    <div class=""error-description"">{System.Net.WebUtility.HtmlEncode(error)}</div>
                                </li>");
            }

            sb.AppendLine(@"                            </ul>");
            sb.AppendLine(@"                        </div>");
        }

        sb.AppendLine(@"                    </div>");
        sb.AppendLine(@"                </article>");

        return sb.ToString();
    }

    private static string GetFooterSection(ValidationSummary summary, string validationProfile)
    {
        return $@"
        <footer class=""report-footer"">
            <div class=""footer-content"">
                <p>Generated by BonniePDF Validation Tool</p>
                <p>Validation Profile: PDF/A-{validationProfile.ToUpper()} | Total Time: {summary.TotalValidationTime.TotalSeconds:F2}s</p>
            </div>
        </footer>";
    }

    private static string GetJavaScriptData(
        List<ValidationResult> results,
        ValidationSummary summary,
        string validationProfile,
        string sourceDirectory)
    {
        var data = new
        {
            generatedAt = DateTime.UtcNow.ToString("o"),
            validationProfile,
            sourceDirectory,
            totalPdfs = summary.TotalPdfs,
            passedCount = summary.PassedCount,
            failedCount = summary.FailedCount,
            errorCount = summary.ErrorCount,
            totalValidationTime = summary.TotalValidationTime.TotalSeconds,
            results = results.Select(r => new
            {
                r.IsSuccess,
                r.IsCompliant,
                r.ComplianceLevel,
                r.ErrorMessage,
                r.Errors,
                r.PdfFileName,
                r.FileSize,
                validatedAt = r.ValidatedAt.ToString("o"),
                validationDuration = r.ValidationDuration.TotalSeconds
            })
        };

        var json = JsonSerializer.Serialize(data, _jsonOptions);

        return $"const validationData = {json};";
    }

    private static string GetJavaScriptLogic()
    {
        return @"
        // Initialize on page load
        document.addEventListener('DOMContentLoaded', function() {
            initializeSearch();
            initializeFilters();
            initializeExpandCollapse();
            updateStatistics();
        });

        // Search functionality
        function initializeSearch() {
            const searchInput = document.getElementById('search-input');
            const clearButton = document.getElementById('clear-search');
            let searchTimeout;

            searchInput.addEventListener('input', function() {
                clearTimeout(searchTimeout);
                searchTimeout = setTimeout(() => {
                    const searchTerm = this.value.toLowerCase().trim();
                    filterBySearch(searchTerm);
                    clearButton.style.display = searchTerm ? 'block' : 'none';
                }, 300);
            });

            clearButton.addEventListener('click', function() {
                searchInput.value = '';
                filterBySearch('');
                this.style.display = 'none';
            });
        }

        function filterBySearch(searchTerm) {
            const cards = document.querySelectorAll('.result-card');
            let visibleCount = 0;

            cards.forEach(card => {
                const filename = card.getAttribute('data-filename');
                const content = card.textContent.toLowerCase();
                
                if (filename.includes(searchTerm) || content.includes(searchTerm)) {
                    card.classList.remove('hidden');
                    visibleCount++;
                } else {
                    card.classList.add('hidden');
                }
            });

            updateVisibleCount(visibleCount);
        }

        // Filter functionality
        function initializeFilters() {
            const filterButtons = document.querySelectorAll('.filter-btn');
            
            filterButtons.forEach(button => {
                button.addEventListener('click', function() {
                    filterButtons.forEach(btn => btn.classList.remove('active'));
                    this.classList.add('active');
                    
                    const filter = this.getAttribute('data-filter');
                    filterByStatus(filter);
                });
            });
        }

        function filterByStatus(status) {
            const cards = document.querySelectorAll('.result-card');
            let visibleCount = 0;

            cards.forEach(card => {
                const cardStatus = card.getAttribute('data-status');
                
                if (status === 'all' || cardStatus === status) {
                    card.classList.remove('hidden');
                    visibleCount++;
                } else {
                    card.classList.add('hidden');
                }
            });

            updateVisibleCount(visibleCount);
        }

        // Expand/Collapse functionality
        function initializeExpandCollapse() {
            document.getElementById('expand-all').addEventListener('click', function() {
                document.querySelectorAll('.result-card').forEach(card => {
                    card.classList.add('expanded');
                });
            });

            document.getElementById('collapse-all').addEventListener('click', function() {
                document.querySelectorAll('.result-card').forEach(card => {
                    card.classList.remove('expanded');
                });
            });
        }

        function toggleCard(header) {
            const card = header.closest('.result-card');
            card.classList.toggle('expanded');
        }

        // Statistics
        function updateStatistics() {
            if (typeof validationData !== 'undefined') {
                console.log('Total PDFs:', validationData.totalPdfs);
                console.log('Passed:', validationData.passedCount);
                console.log('Failed:', validationData.failedCount);
                console.log('Errors:', validationData.errorCount);
            }
        }

        function updateVisibleCount(count) {
            const resultsSection = document.querySelector('.results-section');
            let countDisplay = document.getElementById('visible-count');
            
            if (!countDisplay) {
                countDisplay = document.createElement('div');
                countDisplay.id = 'visible-count';
                countDisplay.style.cssText = 'margin-bottom: 1rem; color: var(--color-text-secondary);';
                resultsSection.insertBefore(countDisplay, resultsSection.querySelector('h2').nextSibling);
            }
            
            countDisplay.textContent = `Showing ${count} of ${validationData.totalPdfs} results`;
        }
        ";
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
