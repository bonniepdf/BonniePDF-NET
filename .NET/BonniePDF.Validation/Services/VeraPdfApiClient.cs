using System.Text.Json;
using BonniePDF.Validation.Configuration;
using BonniePDF.Validation.Models;

namespace BonniePDF.Validation.Services;

/// <summary>
/// HTTP client implementation for VeraPDF API
/// </summary>
public class VeraPdfApiClient
{
    private readonly Configuration.VeraPdfSettings _settings;

    /// <summary>
    /// Initializes a new instance of the VeraPdfApiClient
    /// </summary>
    public VeraPdfApiClient(Configuration.VeraPdfSettings settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    /// <summary>
    /// Checks if the VeraPDF API is running
    /// </summary>
    public async Task<bool> IsApiRunningAsync()
    {
        try
        {
            using var client = CreateHttpClient(TimeSpan.FromSeconds(5));
            
            try
            {
                var response = await client.GetAsync($"{_settings.BaseUrl}/info");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates a PDF file against PDF/A standards
    /// </summary>
    /// <param name="pdfBytes">The PDF file bytes</param>
    /// <param name="fileName">The PDF file name</param>
    /// <param name="profile">The validation profile to use</param>
    /// <returns>The validation result</returns>
    public async Task<ValidationResult> ValidatePdfAsync(byte[] pdfBytes, string fileName, ValidationProfile profile)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            using var client = CreateHttpClient(_settings.Timeout);

            // Create multipart form data request
            using var content = new MultipartFormDataContent();
            using var pdfContent = new ByteArrayContent(pdfBytes);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(pdfContent, "file", fileName);

            // Convert profile to API format
            string profileId = GetProfileId(profile);
            string validateUrl = $"{_settings.BaseUrl}/validate/{profileId}";
            
            // Request JSON response
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var response = await client.PostAsync(validateUrl, content);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return ValidationResult.CreateFailure(
                    fileName,
                    "", // FilePath will be set by caller
                    pdfBytes.LongLength,
                    stopwatch.Elapsed,
                    $"HTTP {response.StatusCode}: {errorContent}",
                    null
                );
            }

            // Parse the response
            string responseContent = await response.Content.ReadAsStringAsync();
            
            // Check if response is XML or JSON
            if (responseContent.TrimStart().StartsWith("<"))
            {
                return ParseXmlResponse(responseContent, fileName, pdfBytes.LongLength, stopwatch.Elapsed);
            }
            else
            {
                return ParseJsonResponse(responseContent, fileName, pdfBytes.LongLength, stopwatch.Elapsed, profile);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ValidationResult.CreateFailure(
                fileName,
                "", // FilePath will be set by caller
                pdfBytes.LongLength,
                stopwatch.Elapsed,
                ex.Message,
                null
            );
        }
    }

    private HttpClient CreateHttpClient(TimeSpan timeout)
    {
        var client = new HttpClient
        {
            Timeout = timeout
        };
        return client;
    }

    private string GetProfileId(ValidationProfile profile)
    {
        return profile switch
        {
            ValidationProfile.PDF_A_1b => "1b",
            ValidationProfile.PDF_A_2b => "2b",
            ValidationProfile.PDF_A_3b => "3b",
            ValidationProfile.PDF_A_1a => "1a",
            ValidationProfile.PDF_A_2a => "2a",
            ValidationProfile.PDF_A_3a => "3a",
            _ => "1b"
        };
    }

    private ValidationResult ParseJsonResponse(
        string jsonResponse,
        string fileName,
        long fileSize,
        TimeSpan validationDuration,
        ValidationProfile profile)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            JsonElement root = jsonDoc.RootElement;
            
            bool isCompliant = false;
            string complianceLevel = $"PDF/A-{GetProfileId(profile).ToUpper()}";
            var errors = new List<string>();
            
            if (root.TryGetProperty("report", out JsonElement report) &&
                report.TryGetProperty("jobs", out JsonElement jobs) &&
                jobs.GetArrayLength() > 0)
            {
                JsonElement firstJob = jobs[0];
                
                if (firstJob.TryGetProperty("validationResult", out JsonElement validationResult) &&
                    validationResult.GetArrayLength() > 0)
                {
                    JsonElement firstResult = validationResult[0];
                    
                    if (firstResult.TryGetProperty("compliant", out JsonElement compliant))
                    {
                        isCompliant = compliant.GetBoolean();
                    }
                    
                    if (firstResult.TryGetProperty("profileName", out JsonElement profileName))
                    {
                        complianceLevel = profileName.GetString() ?? complianceLevel;
                    }
                    
                    if (!isCompliant && firstResult.TryGetProperty("details", out JsonElement details))
                    {
                        if (details.TryGetProperty("ruleSummaries", out JsonElement ruleSummaries))
                        {
                            foreach (JsonElement ruleSummary in ruleSummaries.EnumerateArray())
                            {
                                string? ruleStatus = null;
                                if (ruleSummary.TryGetProperty("ruleStatus", out JsonElement ruleStatusElem))
                                {
                                    ruleStatus = ruleStatusElem.GetString();
                                }
                                
                                if (ruleStatus != null && ruleStatus.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
                                {
                                    string clause = ruleSummary.TryGetProperty("clause", out JsonElement clauseElem) 
                                        ? clauseElem.GetString() ?? "" : "";
                                    string testNumber = ruleSummary.TryGetProperty("testNumber", out JsonElement testNumElem) 
                                        ? testNumElem.GetInt32().ToString() : "";
                                    string description = ruleSummary.TryGetProperty("description", out JsonElement descElem) 
                                        ? descElem.GetString() ?? "" : "";
                                    string specification = ruleSummary.TryGetProperty("specification", out JsonElement specElem) 
                                        ? specElem.GetString() ?? "" : "";
                                    
                                    string errorStr = $"[{specification} {clause}.{testNumber}] {description}";
                                    errors.Add(errorStr);
                                    
                                    if (ruleSummary.TryGetProperty("checks", out JsonElement checks))
                                    {
                                        foreach (JsonElement check in checks.EnumerateArray())
                                        {
                                            if (check.TryGetProperty("status", out JsonElement checkStatus) &&
                                                checkStatus.GetString()?.Equals("failed", StringComparison.OrdinalIgnoreCase) == true)
                                            {
                                                string context = check.TryGetProperty("context", out JsonElement contextElem) 
                                                    ? contextElem.GetString() ?? "" : "";
                                                
                                                if (!string.IsNullOrEmpty(context))
                                                {
                                                    errors.Add($"  └─ Location: {context}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        
                        if (errors.Count == 0)
                        {
                            if (details.TryGetProperty("failedRules", out JsonElement failedRules))
                            {
                                int failedCount = failedRules.GetInt32();
                                if (failedCount > 0)
                                {
                                    errors.Add($"Failed {failedCount} validation rule(s). Use the VeraPDF web interface at http://localhost:8080 for detailed error information.");
                                }
                            }
                        }
                    }
                }
            }

            return ValidationResult.CreateSuccess(
                fileName,
                "", // FilePath will be set by caller
                fileSize,
                validationDuration,
                isCompliant,
                complianceLevel,
                errors,
                jsonResponse
            );
        }
        catch (Exception ex)
        {
            return ValidationResult.CreateFailure(
                fileName,
                "",
                fileSize,
                validationDuration,
                $"Failed to parse JSON response: {ex.Message}",
                jsonResponse
            );
        }
    }

    private ValidationResult ParseXmlResponse(
        string xmlResponse,
        string fileName,
        long fileSize,
        TimeSpan validationDuration)
    {
        try
        {
            bool isCompliant = false;
            
            var complianceMatch = System.Text.RegularExpressions.Regex.Match(
                xmlResponse, 
                @"<compliance>(.*?)</compliance>", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase
            );
            
            if (complianceMatch.Success)
            {
                string complianceValue = complianceMatch.Groups[1].Value.Trim();
                isCompliant = complianceValue.Equals("Passed", StringComparison.OrdinalIgnoreCase) ||
                              complianceValue.Equals("True", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                var passedMatch = System.Text.RegularExpressions.Regex.Match(
                    xmlResponse, 
                    @"<passed>(.*?)</passed>", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );
                
                if (passedMatch.Success)
                {
                    string passedValue = passedMatch.Groups[1].Value.Trim();
                    isCompliant = passedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            
            string complianceLevel = "PDF/A-1B";
            var errors = new List<string>();

            if (!isCompliant)
            {
                var ruleMatches = System.Text.RegularExpressions.Regex.Matches(
                    xmlResponse,
                    @"<rule[^>]*status=""failed""[^>]*>(.*?)</rule>",
                    System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                );

                var processedErrors = new HashSet<string>();
                
                foreach (System.Text.RegularExpressions.Match match in ruleMatches)
                {
                    string ruleContent = match.Groups[1].Value;
                    string ruleTag = match.Value;
                    
                    var messageMatch = System.Text.RegularExpressions.Regex.Match(
                        ruleContent, 
                        @"<message>(.*?)</message>", 
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    string message = messageMatch.Success ? messageMatch.Groups[1].Value.Trim() : "";
                    
                    var locationMatch = System.Text.RegularExpressions.Regex.Match(
                        ruleContent, 
                        @"<location>(.*?)</location>", 
                        System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase
                    );
                    string location = locationMatch.Success ? locationMatch.Groups[1].Value.Trim() : "";
                    
                    var clauseMatch = System.Text.RegularExpressions.Regex.Match(ruleTag, @"clause=""([^""]+)""");
                    string clause = clauseMatch.Success ? clauseMatch.Groups[1].Value : "";
                    
                    var testMatch = System.Text.RegularExpressions.Regex.Match(ruleTag, @"test=""([^""]+)""");
                    string test = testMatch.Success ? testMatch.Groups[1].Value : "";
                    
                    string errorStr = "";
                    if (!string.IsNullOrEmpty(clause) && !string.IsNullOrEmpty(test))
                    {
                        errorStr = $"[{clause} {test}]";
                    }
                    
                    if (!string.IsNullOrEmpty(message))
                    {
                        errorStr += $" {message}";
                    }
                    
                    if (!string.IsNullOrEmpty(location))
                    {
                        errorStr += $" ({location})";
                    }
                    
                    errorStr = errorStr.Trim();
                    
                    if (!string.IsNullOrEmpty(errorStr) && !processedErrors.Contains(errorStr))
                    {
                        errors.Add(errorStr);
                        processedErrors.Add(errorStr);
                    }
                }
                
                if (errors.Count == 0)
                {
                    errors.Add("PDF does not comply with PDF/A-1B standard. Use a validation profile that matches the PDF version or check the detailed report for specific issues.");
                }
            }

            return ValidationResult.CreateSuccess(
                fileName,
                "",
                fileSize,
                validationDuration,
                isCompliant,
                complianceLevel,
                errors,
                xmlResponse
            );
        }
        catch (Exception ex)
        {
            return ValidationResult.CreateFailure(
                fileName,
                "",
                fileSize,
                validationDuration,
                $"Failed to parse XML response: {ex.Message}",
                xmlResponse
            );
        }
    }
}
