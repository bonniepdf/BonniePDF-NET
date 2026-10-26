using BonniePDF;

namespace BonniePDF.Examples.PracticalRecipes;

/// <summary>Generate and write with bounded concurrency, cancellation, and deterministic disposal.</summary>
public static class AsyncBatchExample
{
    public static async Task<int> GenerateAsync<T>(IEnumerable<T> records, Func<T, Document> build,
        Func<T, byte[], CancellationToken, Task> write, int concurrency = 2, CancellationToken cancellationToken = default)
    {
        if (concurrency is < 1 or > 16) throw new ArgumentOutOfRangeException(nameof(concurrency));
        int completed = 0;
        await Parallel.ForEachAsync(records, new ParallelOptions { MaxDegreeOfParallelism = concurrency, CancellationToken = cancellationToken }, async (record, token) =>
        {
            using var doc = build(record);
            byte[] bytes = await doc.GenerateBytesAsync(cancellationToken: token).ConfigureAwait(false);
            if (doc.Diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)) throw new InvalidOperationException("PDF reported a problem.");
            await write(record, bytes, token).ConfigureAwait(false);
            Interlocked.Increment(ref completed);
        }).ConfigureAwait(false);
        return completed;
    }
}
