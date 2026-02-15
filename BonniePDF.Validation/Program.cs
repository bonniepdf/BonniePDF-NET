using System.Text.Json;

namespace BonniePDF.Validation;

internal class Program
{
    private const string VeraPdfBaseUrl = "http://localhost:8080/api";
    private const string DefaultValidationProfile = "1b"; // PDF/A-1B
    
    private static async Task Main(string[] args)
    {
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║           BonniePDF - PDF/A Validation Tool                  ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        // Get the PDF directory to validate
        string pdfDirectory;
        
        if (args.Length > 0 && Directory.Exists(args[0]))
        {
            pdfDirectory = args[0];
        }
        else
        {
            // Default to the examples output directory
            pdfDirectory = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..", "..", "..", "..", "BonniePDF.Examples", "bin", "Debug", "net8.0", "GeneratedPdfs"
            );
            
            // Try to resolve the absolute path
            pdfDirectory = Path.GetFullPath(pdfDirectory);
            
            // If the default directory doesn't exist, ask the user
            if (!Directory.Exists(pdfDirectory))
            {
                Console.WriteLine("Default PDF directory not found.");
                Console.Write("Enter the path to the directory containing PDFs to validate: ");
                pdfDirectory = Console.ReadLine() ?? string.Empty;
                
                if (string.IsNullOrEmpty(pdfDirectory) || !Directory.Exists(pdfDirectory))
                {
                    Console.WriteLine("Invalid directory. Exiting.");
                    return;
                }
            }
        }

        Console.WriteLine($"Validating PDFs in: {pdfDirectory}");
        Console.WriteLine($"VeraPDF API: {VeraPdfBaseUrl}");
        Console.WriteLine($"Validation Profile: PDF/A-{DefaultValidationProfile.ToUpper()}");
        Console.WriteLine();

        // Check if VeraPDF API is accessible
        if (!await IsVeraPdfApiRunningAsync())
        {
            Console.WriteLine("✗ Error: Cannot connect to VeraPDF API at http://localhost:8080/");
            Console.WriteLine("  Please ensure the VeraPDF validation service is running.");
            Console.WriteLine();
            WaitForExit();
            return;
        }

        Console.WriteLine("✓ VeraPDF API is accessible");
        Console.WriteLine();

        // Get all PDF files in the directory
        string[] pdfFiles = Directory.GetFiles(pdfDirectory, "*.pdf", SearchOption.TopDirectoryOnly);
        
        if (pdfFiles.Length == 0)
        {
            Console.WriteLine("No PDF files found in the specified directory.");
            Console.WriteLine();
            WaitForExit();
            return;
        }

        Console.WriteLine($"Found {pdfFiles.Length} PDF file(s) to validate");
        Console.WriteLine();

        // Validate each PDF
        var results = new List<ValidationResult>();
        int passedCount = 0;
        int failedCount = 0;
        int errorCount = 0;

        for (int i = 0; i < pdfFiles.Length; i++)
        {
            string pdfFile = pdfFiles[i];
            Console.WriteLine($"[{i + 1}/{pdfFiles.Length}] Validating: {Path.GetFileName(pdfFile)}");
            
            ValidationResult result = await ValidatePdfAsync(pdfFile);
            results.Add(result);

            if (result.IsSuccess)
            {
                if (result.IsCompliant)
                {
                    Console.WriteLine($"   ✓ PASSED - {result.ComplianceLevel}");
                    passedCount++;
                }
                else
                {
                    Console.WriteLine($"   ✗ FAILED - Not compliant");
                    failedCount++;
                    
                    if (result.Errors != null && result.Errors.Count > 0)
                    {
                        Console.WriteLine("   Errors:");
                        foreach (var error in result.Errors)
                        {
                            Console.WriteLine($"     - {error}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"   ✗ ERROR - {result.ErrorMessage}");
                errorCount++;
            }

            Console.WriteLine();
        }

        // Print summary
        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║                      Validation Summary                       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        Console.WriteLine($"Total PDFs validated: {pdfFiles.Length}");
        Console.WriteLine($"  ✓ Passed:  {passedCount}");
        Console.WriteLine($"  ✗ Failed:  {failedCount}");
        Console.WriteLine($"  ⚠ Errors:  {errorCount}");
        Console.WriteLine();

        // Generate report
        if (results.Count > 0)
        {
            string reportPath = Path.Combine(pdfDirectory, "validation-report.json");
            await GenerateReportAsync(results, reportPath);
            Console.WriteLine($"Validation report saved to: {reportPath}");
        }

        Console.WriteLine();
        WaitForExit();
    }

    private static void WaitForExit()
    {
        try
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
        catch (InvalidOperationException)
        {
            // Console input is redirected (e.g., running in VS Code terminal)
            // Just exit without waiting
        }
    }

    private static async Task<bool> IsVeraPdfApiRunningAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            
            // Try to access the API info endpoint to check if the service is running
            try
            {
                var response = await client.GetAsync($"{VeraPdfBaseUrl}/info");
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

    private static async Task<ValidationResult> ValidatePdfAsync(string pdfPath)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5); // PDF validation can take time

            // Read the PDF file
            byte[] pdfBytes = await File.ReadAllBytesAsync(pdfPath);

            // Create multipart form data request
            using var content = new MultipartFormDataContent();
            using var pdfContent = new ByteArrayContent(pdfBytes);
            pdfContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");
            content.Add(pdfContent, "file", Path.GetFileName(pdfPath));

            // Send request to VeraPDF API with validation profile
            string validateUrl = $"{VeraPdfBaseUrl}/validate/{DefaultValidationProfile}";
            
            // Request JSON response
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            
            var response = await client.PostAsync(validateUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                return new ValidationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"HTTP {response.StatusCode}: {errorContent}"
                };
            }

            // Parse the response
            string responseContent = await response.Content.ReadAsStringAsync();
            
            // Check if response is XML or JSON
            if (responseContent.TrimStart().StartsWith("<"))
            {
                // XML response - parse it
                return ParseXmlResponse(responseContent);
            }
            else
            {
                // JSON response
                return ParseJsonResponse(responseContent);
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }

    private static ValidationResult ParseJsonResponse(string jsonResponse)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonResponse);
            JsonElement root = jsonDoc.RootElement;
            
            // Navigate to: report.jobs[0].validationResult[0].compliant
            bool isCompliant = false;
            string complianceLevel = $"PDF/A-{DefaultValidationProfile.ToUpper()}";
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
                    
                    // Get compliant status
                    if (firstResult.TryGetProperty("compliant", out JsonElement compliant))
                    {
                        isCompliant = compliant.GetBoolean();
                    }
                    
                    // Get profile name
                    if (firstResult.TryGetProperty("profileName", out JsonElement profileName))
                    {
                        complianceLevel = profileName.GetString() ?? complianceLevel;
                    }
                    
                    // Get statement
                    if (firstResult.TryGetProperty("statement", out JsonElement statement))
                    {
                        // Could use this for additional info
                    }
                    
                    // Get details for errors if not compliant
                    if (!isCompliant && firstResult.TryGetProperty("details", out JsonElement details))
                    {
                        // Extract rule summaries with detailed error information
                        if (details.TryGetProperty("ruleSummaries", out JsonElement ruleSummaries))
                        {
                            foreach (JsonElement ruleSummary in ruleSummaries.EnumerateArray())
                            {
                                // Check if this rule failed
                                string? ruleStatus = null;
                                if (ruleSummary.TryGetProperty("ruleStatus", out JsonElement ruleStatusElem))
                                {
                                    ruleStatus = ruleStatusElem.GetString();
                                }
                                
                                // Only process failed rules
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
                                    
                                    // Build error string
                                    string errorStr = $"[{specification} {clause}.{testNumber}] {description}";
                                    errors.Add(errorStr);
                                    
                                    // Extract check context if available
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
                        
                        // If no rule summaries found, provide summary info
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

            return new ValidationResult
            {
                IsSuccess = true,
                IsCompliant = isCompliant,
                ComplianceLevel = complianceLevel,
                Errors = errors,
                RawResponse = jsonResponse
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to parse JSON response: {ex.Message}"
            };
        }
    }

    private static ValidationResult ParseXmlResponse(string xmlResponse)
    {
        try
        {
            // Parse the compliance status
            // Look for <compliance>Passed</compliance> or <passed>true</passed>
            bool isCompliant = false;
            
            // Check for compliance element
            var complianceMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"<compliance>(.*?)</compliance>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (complianceMatch.Success)
            {
                string complianceValue = complianceMatch.Groups[1].Value.Trim();
                isCompliant = complianceValue.Equals("Passed", StringComparison.OrdinalIgnoreCase) ||
                              complianceValue.Equals("True", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                // Check for passed element
                var passedMatch = System.Text.RegularExpressions.Regex.Match(xmlResponse, @"<passed>(.*?)</passed>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (passedMatch.Success)
                {
                    string passedValue = passedMatch.Groups[1].Value.Trim();
                    isCompliant = passedValue.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
            }
            
            string complianceLevel = $"PDF/A-{DefaultValidationProfile.ToUpper()}";
            var errors = new List<string>();

            // If not compliant, extract error information
            if (!isCompliant)
            {
                // Look for failed rules in XML with more detailed parsing
                // Format: <rule clause="..." test="..." status="failed" ...>
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
                    
                    // Extract message
                    var messageMatch = System.Text.RegularExpressions.Regex.Match(ruleContent, @"<message>(.*?)</message>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    string message = messageMatch.Success ? messageMatch.Groups[1].Value.Trim() : "";
                    
                    // Extract location
                    var locationMatch = System.Text.RegularExpressions.Regex.Match(ruleContent, @"<location>(.*?)</location>", System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    string location = locationMatch.Success ? locationMatch.Groups[1].Value.Trim() : "";
                    
                    // Extract clause
                    var clauseMatch = System.Text.RegularExpressions.Regex.Match(ruleTag, @"clause=""([^""]+)""");
                    string clause = clauseMatch.Success ? clauseMatch.Groups[1].Value : "";
                    
                    // Extract test
                    var testMatch = System.Text.RegularExpressions.Regex.Match(ruleTag, @"test=""([^""]+)""");
                    string test = testMatch.Success ? testMatch.Groups[1].Value : "";
                    
                    // Build error string
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
                    
                    // Avoid duplicate errors
                    if (!string.IsNullOrEmpty(errorStr) && !processedErrors.Contains(errorStr))
                    {
                        errors.Add(errorStr);
                        processedErrors.Add(errorStr);
                    }
                }
                
                // If no detailed errors found, provide a generic message
                if (errors.Count == 0)
                {
                    errors.Add("PDF does not comply with PDF/A-1B standard. Use a validation profile that matches the PDF version or check the detailed report for specific issues.");
                }
            }

            return new ValidationResult
            {
                IsSuccess = true,
                IsCompliant = isCompliant,
                ComplianceLevel = complianceLevel,
                Errors = errors,
                RawResponse = xmlResponse
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                IsSuccess = false,
                ErrorMessage = $"Failed to parse XML response: {ex.Message}"
            };
        }
    }

    private static async Task GenerateReportAsync(List<ValidationResult> results, string reportPath)
    {
        var report = new
        {
            GeneratedAt = DateTime.UtcNow.ToString("o"),
            TotalPdfs = results.Count,
            Passed = results.FindAll(r => r.IsSuccess && r.IsCompliant).Count,
            Failed = results.FindAll(r => r.IsSuccess && !r.IsCompliant).Count,
            Errors = results.FindAll(r => !r.IsSuccess).Count,
            Results = results.Select(r => new
            {
                r.IsSuccess,
                r.IsCompliant,
                r.ComplianceLevel,
                r.ErrorMessage,
                r.Errors
            })
        };

        string json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(reportPath, json);
    }

    private class ValidationResult
    {
        public bool IsSuccess { get; set; }
        public bool IsCompliant { get; set; }
        public string? ComplianceLevel { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string>? Errors { get; set; }
        public string? RawResponse { get; set; }
    }
}
