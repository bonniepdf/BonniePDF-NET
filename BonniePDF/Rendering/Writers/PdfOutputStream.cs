using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BonniePDF.Rendering.Writers;

/// <summary>
/// The stream the PDF writers write through. It buffers output and counts the bytes written,
/// so object offsets for the cross-reference table come from the count rather than from the
/// destination's position: the destination need not be seekable (an HTTP response body or a
/// pipe works), and writing an object no longer flushes the destination.
/// </summary>
/// <remarks>
/// The count starts at the destination's position when it has one, so offsets are exactly what
/// the destination's own position would have reported. <see cref="Flush"/> deliberately does
/// nothing, because the writers' <see cref="StreamWriter"/> calls it before every object;
/// <see cref="Complete"/> pushes the buffered bytes to the destination and flushes it.
/// </remarks>
internal sealed class PdfOutputStream : Stream
{
    private const int BufferSize = 64 * 1024;
    private readonly Stream _destination;
    private byte[]? _buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
    private int _buffered;
    private long _position;

    internal PdfOutputStream(Stream destination)
    {
        _destination = destination;
        _position = destination.CanSeek ? destination.Position : 0;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    /// <summary>The offset of the next byte written, as the destination's position would report it.</summary>
    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> data)
    {
        byte[] buffer = Buffer();
        if (_buffered + data.Length > buffer.Length)
        {
            WriteBuffered();
            if (data.Length >= buffer.Length)
            {
                _destination.Write(data);
                _position += data.Length;
                return;
            }
        }
        data.CopyTo(buffer.AsSpan(_buffered));
        _buffered += data.Length;
        _position += data.Length;
    }

    public override void WriteByte(byte value) => Write([value]);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        byte[] buffer = Buffer();
        if (_buffered + data.Length > buffer.Length)
        {
            await WriteBufferedAsync(cancellationToken).ConfigureAwait(false);
            if (data.Length >= buffer.Length)
            {
                await _destination.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                _position += data.Length;
                return;
            }
        }
        data.Span.CopyTo(buffer.AsSpan(_buffered));
        _buffered += data.Length;
        _position += data.Length;
    }

    /// <summary>Does nothing; see the class remarks. Use <see cref="Complete"/> at the end.</summary>
    public override void Flush()
    {
    }

    /// <summary>Does nothing; see the class remarks. Use <see cref="CompleteAsync"/> at the end.</summary>
    public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>Writes the buffered bytes to the destination and flushes it.</summary>
    internal void Complete()
    {
        WriteBuffered();
        _destination.Flush();
    }

    /// <summary>Writes the buffered bytes to the destination and flushes it.</summary>
    internal async ValueTask CompleteAsync(CancellationToken cancellationToken = default)
    {
        await WriteBufferedAsync(cancellationToken).ConfigureAwait(false);
        await _destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <summary>Returns the buffer to the pool. The destination belongs to the writer.</summary>
    protected override void Dispose(bool disposing)
    {
        if (_buffer is { } buffer)
        {
            _buffer = null;
            ArrayPool<byte>.Shared.Return(buffer);
        }
        base.Dispose(disposing);
    }

    private byte[] Buffer() => _buffer ?? throw new ObjectDisposedException(nameof(PdfOutputStream));

    private void WriteBuffered()
    {
        if (_buffered == 0) return;
        _destination.Write(Buffer(), 0, _buffered);
        _buffered = 0;
    }

    private async ValueTask WriteBufferedAsync(CancellationToken cancellationToken)
    {
        if (_buffered == 0) return;
        await _destination.WriteAsync(Buffer().AsMemory(0, _buffered), cancellationToken).ConfigureAwait(false);
        _buffered = 0;
    }
}
