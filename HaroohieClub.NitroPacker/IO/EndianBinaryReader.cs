using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HaroohieClub.NitroPacker.IO;

/// <summary>
/// Basic binary stream reader class
/// </summary>
public class EndianBinaryReader : IDisposable
{
    private bool _disposed;
    private byte[] _buffer;

    /// <summary>
    /// The underlying stream the binary reader is reading
    /// </summary>
    public Stream BaseStream { get; }
    /// <summary>
    /// The endianness of the reader (big vs little)
    /// </summary>
    public Endianness Endianness { get; }

    private static Endianness SystemEndianness =>
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

    private bool Reverse => SystemEndianness != Endianness;

    /// <summary>
    /// Constructs an endian binary reader from the given properties
    /// </summary>
    /// <param name="baseStream">The stream to read</param>
    /// <param name="endianness">The endianness of the given stream (defaults to little endian)</param>
    /// <exception cref="ArgumentNullException">Thrown if the stream is null</exception>
    /// <exception cref="ArgumentException">Thrown if the stream cannot be read</exception>
    public EndianBinaryReader(Stream baseStream, Endianness endianness = Endianness.LittleEndian)
    {
        if (baseStream == null)
            throw new ArgumentNullException(nameof(baseStream));
        if (!baseStream.CanRead)
            throw new ArgumentException(nameof(baseStream));

        BaseStream = baseStream;
        Endianness = endianness;
    }

    ~EndianBinaryReader()
    {
        Dispose(false);
    }

    private void FillBuffer(int bytes, int stride)
    {
        if (_buffer == null || _buffer.Length < bytes)
            _buffer = new byte[bytes];

        BaseStream.Read(_buffer, 0, bytes);

        if (Reverse && stride > 1)
        {
            for (int i = 0; i < bytes; i += stride)
                Array.Reverse(_buffer, i, stride);
        }
    }

    public void Skip(long numBytes)
    {
        BaseStream.Seek(numBytes, SeekOrigin.Current);
    }

    /// <summary>
    /// Reads a single <see cref="char"/> from the stream
    /// </summary>
    /// <param name="encoding">The encoding to use when reading</param>
    /// <returns>A char from the stream</returns>
    public char ReadChar(Encoding encoding)
    {
        int size;

        size = GetEncodingSize(encoding);
        FillBuffer(size, size);
        return encoding.GetChars(_buffer, 0, size)[0];
    }

    /// <summary>
    /// Reads a set of chars from the stream
    /// </summary>
    /// <param name="encoding">The encoding of the characters in the stream</param>
    /// <param name="count">The number of characters to read</param>
    /// <returns>A char array of characters from the strean</returns>
    public char[] ReadChars(Encoding encoding, int count)
    {
        int size;

        size = GetEncodingSize(encoding);
        FillBuffer(size * count, size);
        return encoding.GetChars(_buffer, 0, size * count);
    }

    private static int GetEncodingSize(Encoding encoding)
    {
        if (encoding == Encoding.UTF8 || encoding == Encoding.ASCII)
            return 1;
        else if (encoding == Encoding.Unicode || encoding == Encoding.BigEndianUnicode)
            return 2;

        return 1;
    }

    /// <summary>
    /// Reads a string from the stream in a specified encoding until a null terminator is reached
    /// </summary>
    /// <param name="encoding">The encoding of the string</param>
    /// <returns>The characters found as a string</returns>
    public string ReadStringNT(Encoding encoding)
    {
        string text;

        text = "";

        do
        {
            text += ReadChar(encoding);
        } while (!text.EndsWith("\0", StringComparison.Ordinal));

        return text.Remove(text.Length - 1);
    }

    /// <summary>
    /// Reads a string in a specified encoding of a specified length from the stream
    /// </summary>
    /// <param name="encoding">The encoding to use</param>
    /// <param name="count">The number of characters to read</param>
    /// <returns>A string of the specified length</returns>
    public string ReadString(Encoding encoding, int count)
    {
        return new(ReadChars(encoding, count));
    }

    /// <summary>
    /// Reads an unmanaged type T from the stream
    /// </summary>
    /// <typeparam name="T">The type to read</typeparam>
    /// <returns>An object of type T</returns>
    public unsafe T Read<T>() where T : unmanaged
    {
        int size = sizeof(T);
        FillBuffer(size, size);
        return MemoryMarshal.Read<T>(_buffer);
    }

    /// <summary>
    /// Reads a set of unmanaged types T of a specified length
    /// </summary>
    /// <param name="count">The number of objects to read</param>
    /// <typeparam name="T">The specified type to read</typeparam>
    /// <returns>An array of type T</returns>
    public unsafe T[] Read<T>(int count) where T : unmanaged
    {
        int size = sizeof(T);
        var result = new T[count];
        var byteResult = MemoryMarshal.Cast<T, byte>(result);
        BaseStream.Read(byteResult);

        if (Reverse && size > 1)
        {
            for (int i = 0; i < size * count; i += size)
                byteResult.Slice(i, size).Reverse();
        }

        return result;
    }

    /// <summary>
    /// Reads an fx16 (a 16-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <returns>A double representation of the fixed point number</returns>
    public double ReadFx16()
    {
        return Read<short>() / 4096d;
    }

    /// <summary>
    /// Reads a set of fx16s (16-bit fixed point numbers used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="count">The number of fx16s to read</param>
    /// <returns>An array of doubles corresponding to each fixed point number</returns>
    public double[] ReadFx16s(int count)
    {
        var result = new double[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadFx16();

        return result;
    }

    /// <summary>
    /// Reads an fx32 (a 32-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <returns>A double representation of the fixed point number</returns>
    public double ReadFx32()
    {
        return Read<int>() / 4096d;
    }

    /// <summary>
    /// Reads a set of fx32s (32-bit fixed point numbers used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="count">The number of fx32s to read</param>
    /// <returns>An array of doubles corresponding to each fixed point number</returns>
    public double[] ReadFx32s(int count)
    {
        var result = new double[count];
        for (int i = 0; i < count; i++)
            result[i] = ReadFx32();

        return result;
    }

    /// <summary>
    /// Closes the stream and disposes of the reader
    /// </summary>
    public void Close()
    {
        Dispose();
    }

    /// <summary>
    /// Disposes of the reader and stream without having the GC calling the finalizer of this object
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of this object while optionally not disposing of the base stream
    /// </summary>
    /// <param name="disposing">If true, disposes of the base stream</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
            BaseStream?.Close();

        _buffer = null;
        _disposed = true;
    }
}