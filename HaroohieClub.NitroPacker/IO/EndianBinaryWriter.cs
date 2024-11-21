using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace HaroohieClub.NitroPacker.IO;

/// <summary>
/// Basic binary stream writer class
/// </summary>
public class EndianBinaryWriter : IDisposable
{
    /// <summary>
    /// Indicates whether the writer has been disposed
    /// </summary>
    protected bool Disposed;
    private byte[] _buffer;

    /// <summary>
    /// The underlying stream to write to
    /// </summary>
    public Stream BaseStream { get; private set; }
    /// <summary>
    /// The endianness of the writer (big or little)
    /// </summary>
    public Endianness Endianness { get; set; }

    private static Endianness SystemEndianness =>
        BitConverter.IsLittleEndian ? Endianness.LittleEndian : Endianness.BigEndian;

    private bool Reverse => SystemEndianness != Endianness;

    /// <summary>
    /// Constructs an EndianBinaryWriter from the base stream and with the given endianness
    /// </summary>
    /// <param name="baseStream">The stream to write to</param>
    /// <param name="endianness">The endianness of the stream (defaults to little)</param>
    /// <exception cref="ArgumentNullException">Thrown if the stream is null</exception>
    /// <exception cref="ArgumentException">Thrown if the stream cannot be written too</exception>
    public EndianBinaryWriter(Stream baseStream, Endianness endianness = Endianness.LittleEndian)
    {
        ArgumentNullException.ThrowIfNull(baseStream);
        if (!baseStream.CanWrite)
            throw new ArgumentException(nameof(baseStream));

        BaseStream = baseStream;
        Endianness = endianness;
    }

    ~EndianBinaryWriter()
    {
        Dispose(false);
    }

    private void WriteBuffer(int bytes, int stride)
    {
        if (Reverse && stride > 1)
        {
            for (int i = 0; i < bytes; i += stride)
                Array.Reverse(_buffer, i, stride);
        }

        BaseStream.Write(_buffer, 0, bytes);
    }

    private void CreateBuffer(int size)
    {
        if (_buffer == null || _buffer.Length < size)
            _buffer = new byte[size];
    }

    /// <summary>
    /// Seeks past a certain number of bytes in the stream
    /// </summary>
    /// <param name="numBytes">The number of bytes to skip</param>
    public void Skip(long numBytes)
    {
        BaseStream.Seek(numBytes, SeekOrigin.Current);
    }

    /// <summary>
    /// Writes a byte to the stream
    /// </summary>
    /// <param name="value">The byte to write</param>
    public void Write(byte value)
    {
        CreateBuffer(1);
        _buffer[0] = value;
        WriteBuffer(1, 1);
    }

    /// <summary>
    /// Writes a character to the stream in a given encoding
    /// </summary>
    /// <param name="value">The character to write to the stream</param>
    /// <param name="encoding">The encoding to encode the character in</param>
    public void Write(char value, Encoding encoding)
    {
        int size;

        size = GetEncodingSize(encoding);
        CreateBuffer(size);
        Array.Copy(encoding.GetBytes(new string(value, 1)), 0, _buffer, 0, size);
        WriteBuffer(size, size);
    }

    /// <summary>
    /// Writes a char array to the stream in a given encoding
    /// </summary>
    /// <param name="value">The char array to write</param>
    /// <param name="offset">The offset into the char array to write from</param>
    /// <param name="count">The number of characters to write</param>
    /// <param name="encoding">The encoding to write in</param>
    public void Write(char[] value, int offset, int count, Encoding encoding)
    {
        int size;

        size = GetEncodingSize(encoding);
        CreateBuffer(size * count);
        Array.Copy(encoding.GetBytes(value, offset, count), 0, _buffer, 0, count * size);
        WriteBuffer(size * count, size);
    }

    private static int GetEncodingSize(Encoding encoding)
    {
        if (encoding.Equals(Encoding.UTF8) || encoding.Equals(Encoding.ASCII))
            return 1;
        if (encoding.Equals(Encoding.Unicode) || encoding.Equals(Encoding.BigEndianUnicode))
            return 2;

        return 1;
    }

    /// <summary>
    /// Writes an optionally null terminated string to the stream in a specified encoding
    /// </summary>
    /// <param name="value">The string to write to the stream</param>
    /// <param name="encoding">The encoding to encode the string in</param>
    /// <param name="nullTerminated">Whether to null-terminate the string or not</param>
    public void Write(string value, Encoding encoding, bool nullTerminated)
    {
        Write(value.ToCharArray(), 0, value.Length, encoding);
        if (nullTerminated)
            Write('\0', encoding);
    }

    /// <summary>
    /// Writes an unmanaged object of type T to the stream
    /// </summary>
    /// <param name="value">The object to write</param>
    /// <typeparam name="T">The type of that object</typeparam>
    public unsafe void Write<T>(T value) where T : unmanaged
    {
        int size = sizeof(T);
        CreateBuffer(size);
        MemoryMarshal.Write(_buffer, ref value);
        WriteBuffer(size, size);
    }

    /// <summary>
    /// Writes an array of unmanaged type T
    /// </summary>
    /// <param name="value">The array of objects to write to the stream</param>
    /// <typeparam name="T">The unmanaged type of those objects</typeparam>
    public unsafe void Write<T>(T[] value) where T : unmanaged
        => Write<T>(value.AsSpan());

    /// <summary>
    /// Writes a specified number of objects starting at a particular index in a provided array to the stream
    /// </summary>
    /// <param name="value">The array of objects to write from</param>
    /// <param name="offset">The index to into the array to start writing from</param>
    /// <param name="count">The number of objects to write</param>
    /// <typeparam name="T">The unmanaged type of the objects</typeparam>
    public unsafe void Write<T>(T[] value, int offset, int count) where T : unmanaged
        => Write<T>(value.AsSpan(offset, count));

    /// <summary>
    /// Writes a span of objects to the stream
    /// </summary>
    /// <param name="value">The ReadOnlySpan of objects</param>
    /// <typeparam name="T">The unamanged type of the objects</typeparam>
    public unsafe void Write<T>(ReadOnlySpan<T> value) where T : unmanaged
    {
        int size = sizeof(T);

        if (!Reverse || size == 1)
        {
            BaseStream.Write(MemoryMarshal.Cast<T, byte>(value));
            return;
        }

        CreateBuffer(size * value.Length);
        MemoryMarshal.Cast<T, byte>(value).CopyTo(_buffer);
        WriteBuffer(size * value.Length, size);
    }

    /// <summary>
    /// Writes an fx16 (a 16-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="value">A double value to be reinterpreted as an fx16 and written to the stream</param>
    public void WriteFx16(double value)
    {
        Write((short)Math.Round(value * 4096d));
    }

    /// <summary>
    /// Writes an array of fx16s (a 16-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="values">An array of double values to be reinterpreted as fx16s and written to the stream</param>
    public void WriteFx16s(ReadOnlySpan<double> values)
    {
        for (int i = 0; i < values.Length; i++)
            WriteFx16(values[i]);
    }

    /// <summary>
    /// Writes an fx32 (a 32-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="value">A double value to be reinterpreted as an fx32 and written to the stream</param>
    public void WriteFx32(double value)
    {
        Write((int)Math.Round(value * 4096d));
    }

    /// <summary>
    /// Writes an array of fx32s (a 32-bit fixed point number used on the Nintendo DS instead of floating point math)
    /// </summary>
    /// <param name="values">An array of double values to be reinterpreted as fx32s and written to the stream</param>
    public void WriteFx32s(ReadOnlySpan<double> values)
    {
        for (int i = 0; i < values.Length; i++)
            WriteFx32(values[i]);
    }

    /// <summary>
    /// Closes the stream and disposes of the writer
    /// </summary>
    public void Close()
    {
        Dispose();
    }

    /// <summary>
    /// Closes the stream and disposes of the writer
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of the writer optionally not closing the base stream
    /// </summary>
    /// <param name="disposing">If true, closes the stream</param>
    protected virtual void Dispose(bool disposing)
    {
        if (Disposed)
            return;

        if (disposing)
            BaseStream?.Close();

        _buffer = null;
        Disposed = true;
    }
}