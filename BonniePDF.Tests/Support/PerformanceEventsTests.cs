using System.Diagnostics.Tracing;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace BonniePDF.Tests.Support;

public sealed partial class PerformanceEventsTests
{
    private sealed class Listener : EventListener
    {
        internal readonly List<int> Events = [];
        private readonly int _thread = Environment.CurrentManagedThreadId;
        protected override void OnEventSourceCreated(EventSource source)
        {
            if (source.Name == "BonniePDF-Performance") EnableEvents(source, EventLevel.Verbose);
        }
        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            if (Environment.CurrentManagedThreadId == _thread) Events.Add(data.EventId);
        }
    }

    [Fact]
    public void ExceptionalExitEmitsBalancedEvents()
    {
        using var listener = new Listener();
        Assert.Throws<InvalidOperationException>((Action)(() =>
        {
            using var outer = PerformanceEvents.Measure(PerformanceStage.Measurement);
            using var inner = PerformanceEvents.Measure(PerformanceStage.Shaping);
            throw new InvalidOperationException();
        }));
        Assert.Equal(new[] { 1, 1, 2, 2 }, listener.Events);
    }

    [Fact]
    public void EnablingProfilingDoesNotChangePdfBytes()
    {
        byte[] Generate()
        {
            var document = new BonniePDF.Document();
            document.Metadata.CreationDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
            document.Metadata.ModifiedDate = document.Metadata.CreationDate;
            document.Add(new TextBlock("Profile output invariance"));
            using var stream = new MemoryStream();
            document.Save(stream, leaveOpen: true);
            return stream.ToArray();
        }
        string Normalize(byte[] bytes) => FileIdentifierRegex().Replace(Encoding.Latin1.GetString(bytes), "/ID [<MASKED> <MASKED>]");
        byte[] before = Generate();
        using var listener = new Listener();
        byte[] after = Generate();
        Assert.NotEmpty(listener.Events);
        Assert.Equal(Normalize(before), Normalize(after));
    }

    [GeneratedRegex(@"/ID \[<[0-9A-F]*> <[0-9A-F]*>\]")]
    private static partial Regex FileIdentifierRegex();
}
