using System;
using System.IO;
using System.Text;

namespace BonniePDF.Rendering.Images;

/// <summary>
/// A helper class for reading big-endian binary data from a stream.
/// TTF files and other binary formats use big-endian byte order, while C# uses little-endian.
/// </summary>
public sealed class BigEndianReader : IDisposable
{
    private readonly BinaryReader _reader;

    /// <summary>
    /// Creates a new BigEndianReader for the specified stream.
    /// </summary>
    /// <param name="stream">The stream to read from</param>
    public BigEndianReader(Stream stream)
    {
        _reader = new BinaryReader(stream);
    }

    /// <summary>
    /// Gets or sets the position within the stream.
    /// </summary>
    public long Position => _reader.BaseStream.Position;

    /// <summary>
    /// Disposes the reader and underlying stream.
    /// </summary>
    public void Dispose()
    {
        _reader.Dispose();
    }

    /// <summary>
    /// Sets the position within the stream.
    /// </summary>
    /// <param name="position">The position to seek to</param>
    public void Seek(long position)
    {
        _reader.BaseStream.Seek(position, SeekOrigin.Begin);
    }

    /// <summary>
    /// Sets the position within the stream.
    /// </summary>
    /// <param name="position">The position to seek to</param>
    public void Seek(uint position)
    {
        _reader.BaseStream.Seek(position, SeekOrigin.Begin);
    }

    /// <summary>
    /// Skips ahead in the stream by the specified number of bytes.
    /// </summary>
    /// <param name="count">The number of bytes to skip</param>
    public void Skip(int count)
    {
        _reader.BaseStream.Seek(count, SeekOrigin.Current);
    }

    /// <summary>
    /// Reads the specified number of bytes from the stream.
    /// </summary>
    /// <param name="count">The number of bytes to read</param>
    /// <returns>An array of bytes</returns>
    public byte[] ReadBytes(int count)
    {
        return _reader.ReadBytes(count);
    }

    /// <summary>
    /// Reads a 2-byte unsigned integer (big-endian).
    /// </summary>
    /// <returns>The unsigned 16-bit integer value</returns>
    public ushort ReadUInt16()
    {
        byte[] bytes = _reader.ReadBytes(2);
        Array.Reverse(bytes);
        return BitConverter.ToUInt16(bytes, 0);
    }

    /// <summary>
    /// Reads a 2-byte signed integer (big-endian).
    /// </summary>
    /// <returns>The signed 16-bit integer value</returns>
    public short ReadInt16()
    {
        byte[] bytes = _reader.ReadBytes(2);
        Array.Reverse(bytes);
        return BitConverter.ToInt16(bytes, 0);
    }

    /// <summary>
    /// Reads a 4-byte unsigned integer (big-endian).
    /// </summary>
    /// <returns>The unsigned 32-bit integer value</returns>
    public uint ReadUInt32()
    {
        byte[] bytes = _reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    /// Reads a 4-byte signed integer (big-endian).
    /// </summary>
    /// <returns>The signed 32-bit integer value</returns>
    public int ReadInt32()
    {
        byte[] bytes = _reader.ReadBytes(4);
        Array.Reverse(bytes);
        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>
    /// Reads a 4-byte fixed-point number (big-endian).
    /// </summary>
    /// <returns>The floating-point value</returns>
    public float ReadFixed()
    {
        int value = ReadInt32();
        return value / 65536.0f;
    }

    /// <summary>
    /// Reads a 4-byte ASCII tag.
    /// </summary>
    /// <returns>The tag as a string</returns>
    public string ReadTag()
    {
        return Encoding.ASCII.GetString(_reader.ReadBytes(4));
    }
}