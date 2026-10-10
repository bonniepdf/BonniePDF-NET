using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;

namespace BonniePDF;

// Opt-in synchronous CPU scopes. Consumers must maintain a stack per thread and
// subtract nested scopes when calculating exclusive time. These are diagnostic
// implementation details, not document settings or a supported public API.
[EventSource(Name = "BonniePDF-Performance")]
internal sealed class PerformanceEvents : EventSource
{
    internal static readonly PerformanceEvents Log = new();

    [NonEvent]
    internal static Scope Measure(PerformanceStage stage) =>
        Log.IsEnabled() ? new Scope(stage) : default;

    [Event(1, Level = EventLevel.Verbose)]
    public void Enter(long stage, long timestamp) => WriteEvent(1, stage, timestamp);

    [Event(2, Level = EventLevel.Verbose)]
    public void Leave(long stage, long timestamp) => WriteEvent(2, stage, timestamp);

    internal readonly struct Scope : IDisposable
    {
        private readonly PerformanceStage _stage;

        internal Scope(PerformanceStage stage)
        {
            _stage = stage;
            Log.Enter((long)stage, Stopwatch.GetTimestamp());
        }

        public void Dispose()
        {
            if (_stage != 0) Log.Leave((long)_stage, Stopwatch.GetTimestamp());
        }
    }
}

internal enum PerformanceStage
{
    Measurement = 1,
    FontLoading = 2,
    Shaping = 3,
    FontEmbedding = 4,
    ImageDecoding = 5,
    ImageResizing = 6,
    ImageCompression = 7,
    Painting = 8,
    PdfWriting = 9,
}
