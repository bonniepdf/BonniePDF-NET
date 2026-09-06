using System.Net.Http.Headers;
using System.Text.Json;
using BonniePDF.Tests.Accessibility;
using Xunit;

namespace BonniePDF.Tests.Compliance;

/// <summary>
/// Opt-in VeraPDF integration test. Runs the
/// tagged simple-document fixture through a local VeraPDF REST
/// service (the same one <c>BonniePDF.Validation</c> uses) and
/// asserts zero failed rules under profile <c>ua1</c>.
///
/// The test is opt-in via the <c>VERAPDF=1</c> environment variable
/// because it requires:
///   - a VeraPDF REST container running on <c>localhost:8080</c>, e.g.
///     <c>docker run -d -p 8080:8080 verapdf/rest:latest</c>;
///   - outbound network is not required (the service is local), but
///     the container image must already be present.
///
/// Without <c>VERAPDF=1</c> the test is skipped - keeps day-to-day
/// <c>dotnet test</c> runs fast and offline.
/// </summary>
public class VeraPdfStageZeroCloseoutTests
{
    private const string VeraPdfBaseUrl = "http://localhost:8080/api";

    [Fact]
    public async Task TaggedSimpleDocument_PassesVeraPdfUa1()
    {
        if (Environment.GetEnvironmentVariable("VERAPDF") != "1")
        {
            // xUnit 2.x has no Assert.Skip - treat unset as a no-op so
            // day-to-day `dotnet test` runs stay fast and offline. The
            // test only does meaningful work when explicitly opted in.
            return;
        }

        byte[] pdfBytes = TaggedSimpleDocumentTests.GenerateBytes();

        using HttpClient http = new() { Timeout = TimeSpan.FromMinutes(2) };

        // Confirm the service is reachable so we don't fail with a
        // misleading "connection refused" later.
        HttpResponseMessage info;
        try
        {
            info = await http.GetAsync($"{VeraPdfBaseUrl}/info").ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            Assert.Fail($"Cannot reach VeraPDF REST at {VeraPdfBaseUrl}. Start it with " +
                        $"`docker run -d -p 8080:8080 verapdf/rest:latest`. Underlying error: {ex.Message}");
            return;
        }
        Assert.True(info.IsSuccessStatusCode, $"VeraPDF /info returned {info.StatusCode}");

        // POST the PDF to /validate/ua1.
        using var content = new MultipartFormDataContent();
        using var pdf = new ByteArrayContent(pdfBytes);
        pdf.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(pdf, "file", "simple-document-tagged.pdf");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await http.PostAsync($"{VeraPdfBaseUrl}/validate/ua1", content).ConfigureAwait(false);
        Assert.True(response.IsSuccessStatusCode,
            $"VeraPDF /validate/ua1 returned {response.StatusCode}: {await response.Content.ReadAsStringAsync()}");

        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        ValidationVerdict verdict = ParseValidationVerdict(json);

        Assert.True(verdict.Compliant,
            $"VeraPDF reported {verdict.FailedRuleSummaries.Count} failed rule(s) under profile ua1:\n" +
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
