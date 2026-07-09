using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace BonniePDF.Tests.Compliance;

/// <summary>
/// this feature stage-1 closeout - opt-in VeraPDF integration tests for the
/// three exemplar scenarios (business-invoice, multi-page-flow,
/// dickens-novel). Mirrors <see cref="VeraPdfStageZeroCloseoutTests"/>
/// - same opt-in gating (<c>VERAPDF=1</c>) + local VeraPDF REST
/// service on <c>localhost:8080</c>.
///
/// Document construction is shared with <see cref="Stage1CloseoutTests"/>
/// via internal helpers, so the same fixtures the byte-level tests
/// run against go through real rule-by-rule validation here.
/// </summary>
public class VeraPdfStageOneCloseoutTests
{
    private const string VeraPdfBaseUrl = "http://localhost:8080/api";

    [Fact]
    public Task BusinessInvoice_PassesVeraPdfUa1() =>
        ValidateAsync(Stage1CloseoutTests.BuildBusinessInvoiceDocument(), "business-invoice-tagged.pdf");

    [Fact]
    public Task MultiPageFlow_PassesVeraPdfUa1() =>
        ValidateAsync(Stage1CloseoutTests.BuildMultiPageFlowDocument(), "multi-page-flow-tagged.pdf");

    [Fact]
    public Task DickensNovel_PassesVeraPdfUa1() =>
        ValidateAsync(Stage1CloseoutTests.BuildDickensNovelDocument(), "dickens-novel-tagged.pdf");

    private static async Task ValidateAsync(BonniePDF.Document doc, string filename)
    {
        if (Environment.GetEnvironmentVariable("VERAPDF") != "1")
        {
            // xUnit 2.x has no Assert.Skip - treat unset as a no-op so
            // day-to-day `dotnet test` runs stay fast and offline.
            doc.Dispose();
            return;
        }

        byte[] pdfBytes;
        using (doc)
        using (var ms = new MemoryStream())
        {
            doc.Save(ms, true);
            pdfBytes = ms.ToArray();
        }

        using HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };

        try
        {
            HttpResponseMessage info = await http.GetAsync($"{VeraPdfBaseUrl}/info").ConfigureAwait(false);
            Assert.True(info.IsSuccessStatusCode, $"VeraPDF /info returned {info.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Cannot reach VeraPDF REST at {VeraPdfBaseUrl}. Start it with " +
                        $"`docker run -d -p 8080:8080 verapdf/rest:latest`. Underlying error: {ex.Message}");
            return;
        }

        using var content = new MultipartFormDataContent();
        using var pdf = new ByteArrayContent(pdfBytes);
        pdf.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(pdf, "file", filename);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await http.PostAsync($"{VeraPdfBaseUrl}/validate/ua1", content).ConfigureAwait(false);
        Assert.True(response.IsSuccessStatusCode,
            $"VeraPDF /validate/ua1 returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        ValidationVerdict verdict = ParseValidationVerdict(json);

        Assert.True(verdict.Compliant,
            $"VeraPDF reported {verdict.FailedRuleSummaries.Count} failed rule(s) on {filename}:\n" +
            string.Join("\n", verdict.FailedRuleSummaries));
    }

    private sealed record ValidationVerdict(bool Compliant, System.Collections.Generic.IList<string> FailedRuleSummaries);

    private static ValidationVerdict ParseValidationVerdict(string json)
    {
        var failures = new System.Collections.Generic.List<string>();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        bool compliant = false;
        if (root.TryGetProperty("report", out JsonElement report) &&
            report.TryGetProperty("jobs", out JsonElement jobs) &&
            jobs.GetArrayLength() > 0)
        {
            JsonElement firstJob = jobs[0];
            if (firstJob.TryGetProperty("validationResult", out JsonElement validationResult) &&
                validationResult.GetArrayLength() > 0)
            {
                JsonElement firstResult = validationResult[0];
                if (firstResult.TryGetProperty("compliant", out JsonElement compliantProp))
                    compliant = compliantProp.GetBoolean();

                if (!compliant && firstResult.TryGetProperty("details", out JsonElement details) &&
                    details.TryGetProperty("ruleSummaries", out JsonElement ruleSummaries))
                {
                    foreach (JsonElement rule in ruleSummaries.EnumerateArray())
                    {
                        if (!rule.TryGetProperty("ruleStatus", out JsonElement status)) continue;
                        if (!string.Equals(status.GetString(), "FAILED", StringComparison.OrdinalIgnoreCase)) continue;

                        string clause = rule.TryGetProperty("clause", out JsonElement c) ? c.GetString() ?? "" : "";
                        string testNumber = rule.TryGetProperty("testNumber", out JsonElement t) ? t.GetInt32().ToString() : "";
                        string description = rule.TryGetProperty("description", out JsonElement d) ? d.GetString() ?? "" : "";
                        failures.Add($"  [{clause}.{testNumber}] {description}");
                    }
                }
            }
        }

        return new ValidationVerdict(compliant, failures);
    }
}
