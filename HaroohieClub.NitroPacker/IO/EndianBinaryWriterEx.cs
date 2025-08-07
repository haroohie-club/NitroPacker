using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.IO;

/// <summary>
/// An extended version of the <see cref="EndianBinaryWriter"/>
/// </summary>
public class EndianBinaryWriterEx : EndianBinaryWriter
{
    /// <summary>
    /// Constructs an EndianBinaryWriterEx with an underlying stream
    /// </summary>
    /// <param name="baseStream">The stream to write to</param>
    public EndianBinaryWriterEx(Stream baseStream)
        : base(baseStream) { }

    /// <summary>
    /// Constructs an EndianBinaryWriterEx with a stream and a given endianness
    /// </summary>
    /// <param name="baseStream">The stream to write to</param>
    /// <param name="endianness">The endianness of that stream</param>
    public EndianBinaryWriterEx(Stream baseStream, Endianness endianness)
        : base(baseStream, endianness) { }

    /// <summary>
    /// Enum specifying chunk size
    /// </summary>
    public enum ChunkSizeType
    {
        /// <summary>
        /// 8-bit unsigned integer chunks
        /// </summary>
        U8,
        /// <summary>
        /// 16-bit unsigned integer chunks
        /// </summary>
        U16,
        /// <summary>
        /// 32-bit unsigned integer chunks
        /// </summary>
        U32,
        /// <summary>
        /// 64-bit unsigned integer chunks
        /// </summary>
        U64,
    }

    private class Chunk
    {
        public long StartAddress;
        public bool HasSize;
        public int SizeOffset;
        public ChunkSizeType SizeType;
        public int SizeDelta;
    }

    private readonly Stack<Chunk> _chunks = new();

    internal void BeginChunk() => BeginChunk(null);

    internal void BeginChunk(ChunkPointer chunkPointer)
    {
        if (chunkPointer != null)
            WriteChunkPointer(chunkPointer);
        _chunks.Push(new() { StartAddress = BaseStream.Position, HasSize = false });
    }

    internal void BeginChunk(int sizeOffset, ChunkSizeType sizeType = ChunkSizeType.U32)
        => BeginChunk(null, sizeOffset, sizeType);

    internal void BeginChunk(ChunkPointer chunkPointer, int sizeOffset, ChunkSizeType sizeType = ChunkSizeType.U32)
    {
        if (chunkPointer != null)
            WriteChunkPointer(chunkPointer);
        _chunks.Push(new()
        {
            StartAddress = BaseStream.Position,
            HasSize = true,
            SizeOffset = sizeOffset,
            SizeType = sizeType,
            SizeDelta = 0,
        });
    }

    internal void EndChunk()
    {
        Chunk s = _chunks.Pop();
        if (s.HasSize)
        {
            ulong length = (ulong)(BaseStream.Position - s.StartAddress + s.SizeDelta);
            long curpos = BaseStream.Position;
            BaseStream.Position = s.StartAddress + s.SizeOffset;
            switch (s.SizeType)
            {
                case ChunkSizeType.U8:
                    Write((byte)length);
                    break;
                case ChunkSizeType.U16:
                    Write((ushort)length);
                    break;
                case ChunkSizeType.U32:
                    Write((uint)length);
                    break;
                case ChunkSizeType.U64:
                    Write(length);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            BaseStream.Position = curpos;
        }
    }

    internal enum PointerType
    {
        Global,
        ChunkRelative,
        FieldRelative,
    }

    internal class ChunkPointer
    {
        public long PointerAddress;
        public long PointerBase;
    }

    private void WriteChunkPointer(ChunkPointer pointer)
    {
        long curpos = BaseStream.Position;
        BaseStream.Position = pointer.PointerAddress;
        Write((uint)(curpos - pointer.PointerBase));
        BaseStream.Position = curpos;
    }

    internal ChunkPointer WriteChunkPointer(PointerType type)
    {
        long address = BaseStream.Position;
        long ptrBase = 0;
        switch (type)
        {
            case PointerType.ChunkRelative:
                ptrBase = _chunks.Peek().StartAddress;
                break;
            case PointerType.FieldRelative:
                ptrBase = address;
                break;
        }

        Write(0);
        return new() { PointerAddress = address, PointerBase = ptrBase };
    }

    internal void WriteChunkSize(ChunkSizeType sizeType = ChunkSizeType.U32)
    {
        long curPos = BaseStream.Position;
        switch (sizeType)
        {
            case ChunkSizeType.U8:
                Write((byte)0);
                break;
            case ChunkSizeType.U16:
                Write((ushort)0);
                break;
            case ChunkSizeType.U32:
                Write(0);
                break;
            case ChunkSizeType.U64:
                Write((ulong)0);
                break;
        }

        if (_chunks.Count == 0)
            return;
        Chunk curChunk = _chunks.Peek();
        curChunk.HasSize = true;
        curChunk.SizeOffset = (int)(curPos - curChunk.StartAddress);
        curChunk.SizeType = sizeType;
    }

    internal void WriteChunkSize16()
        => WriteChunkSize(ChunkSizeType.U16);
    
    internal long GetCurposRelative()
        => BaseStream.Position - _chunks.Peek().StartAddress;

    internal void WriteCurposRelative(int offset, int delta = 0)
    {
        long curpos = JumpRelative(offset);
        if (_chunks.Count == 0)
            Write((uint)(curpos + delta));
        else
            Write((uint)(curpos - _chunks.Peek().StartAddress + delta));
        BaseStream.Position = curpos;
    }

    internal void WriteCurposRelativeU16(int offset, int delta = 0)
    {
        long curpos = JumpRelative(offset);
        if (_chunks.Count == 0)
            Write((ushort)(curpos + delta));
        else
            Write((ushort)(curpos - _chunks.Peek().StartAddress + delta));
        BaseStream.Position = curpos;
    }

    /// <summary>
    /// Writes alignment padding to the stream
    /// </summary>
    /// <param name="alignment">The byte alignment</param>
    /// <param name="value">The value to fill padding with (default 0x00)</param>
    public void WritePadding(int alignment, byte value = 0)
    {
        while (BaseStream.Position % alignment != 0)
            Write(value);
    }

    internal long JumpRelative(long offset)
    {
        long curPos = BaseStream.Position;
        if (_chunks.Count == 0)
            BaseStream.Position = offset;
        else
            BaseStream.Position = _chunks.Peek().StartAddress + offset;
        return curPos;
    }

    private void AlignForProperty(PropertyInfo field, PropertyAlignment alignment, PropertyType type)
    {
        var alignAttribute = field.GetCustomAttribute<AlignAttribute>();
        if (alignAttribute != null)
            WritePadding(alignAttribute.Alignment);
        else if (alignment == PropertyAlignment.FieldSize)
            WritePadding(SerializationUtil.GetTypeSize(type));
    }

    private void WriteFieldTypeDirect(PropertyType type, object value)
    {
        switch (type)
        {
            case PropertyType.U8:
                Write((byte)value);
                break;
            case PropertyType.S8:
                Write((sbyte)value);
                break;
            case PropertyType.U16:
                Write((ushort)value);
                break;
            case PropertyType.S16:
                Write((short)value);
                break;
            case PropertyType.U32:
                Write((uint)value);
                break;
            case PropertyType.S32:
                Write((int)value);
                break;
            case PropertyType.U64:
                Write((ulong)value);
                break;
            case PropertyType.S64:
                Write((long)value);
                break;
            case PropertyType.Fx16:
                WriteFx16((double)value);
                break;
            case PropertyType.Fx32:
                WriteFx32((double)value);
                break;
            case PropertyType.Float:
                Write((float)value);
                break;
            case PropertyType.Double:
                Write((double)value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private void WriteFieldTypeArrayDirect(PropertyType type, Array value)
    {
        switch (type)
        {
            case PropertyType.U8:
                Write((byte[])value);
                break;
            case PropertyType.S8:
                Write((sbyte[])value);
                break;
            case PropertyType.U16:
                Write((ushort[])value);
                break;
            case PropertyType.S16:
                Write((short[])value);
                break;
            case PropertyType.U32:
                Write((uint[])value);
                break;
            case PropertyType.S32:
                Write((int[])value);
                break;
            case PropertyType.U64:
                Write((ulong[])value);
                break;
            case PropertyType.S64:
                Write((long[])value);
                break;
            case PropertyType.Fx16:
                WriteFx16s((double[])value);
                break;
            case PropertyType.Fx32:
                WriteFx32s((double[])value);
                break;
            case PropertyType.Float:
                Write((float[])value);
                break;
            case PropertyType.Double:
                Write((double[])value);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private Dictionary<object, List<(long address, Chunk chunk, ReferenceAttribute refInfo)>> _references = new();
    private Dictionary<object, long> _refObjAddresses = new();

    private void WritePrimitive<T>(T obj, PropertyInfo property, PropertyAlignment alignment)
    {
        PropertyType fieldType = SerializationUtil.GetPropertyPrimitiveType(property);
        Type trueType = SerializationUtil.FieldTypeToType(fieldType);

        AlignForProperty(property, alignment, fieldType);

        var constAttrib = property.GetCustomAttribute<ConstantAttribute>();
        object fieldValue = constAttrib != null ? constAttrib.Value : property.GetValue(obj);

        object finalValue;
        if (property.PropertyType == typeof(bool))
            finalValue = SerializationUtil.Cast((bool)fieldValue ? 1 : 0, trueType);
        else
            finalValue = SerializationUtil.Cast(fieldValue, trueType);

        WriteFieldTypeDirect(fieldType, finalValue);
    }

    private void WriteArray<T>(T obj, PropertyInfo property, PropertyAlignment alignment)
    {
        var arrSizeAttr = property.GetCustomAttribute<ArraySizeAttribute>();
        if (arrSizeAttr == null)
            throw new SerializationException(
                $"No array size attribute found for field \"{property.Name}\" in \"{typeof(T).Name}\"");
        int size = arrSizeAttr.FixedSize;

        if (size < 0)
            throw new SerializationException("Array size invalid");

        Type elementType = property.PropertyType.GetElementType();

        var value = property.GetValue(obj) as Array;

        if (value == null)
            throw new SerializationException("Field value is null");

        if (value.Length != size)
            throw new SerializationException("Array size invalid");

        //if (elementType == typeof(Vector2d))
        //{
        //    for (int i = 0; i < size; i++)
        //        WriteVector2dDirect(field, (Vector2d)value.GetValue(i));
        //}
        //else if (elementType == typeof(Vector3d))
        //{
        //    for (int i = 0; i < size; i++)
        //        WriteVector3dDirect(field, (Vector3d)value.GetValue(i));
        //}
        //else 
        if (SerializationUtil.HasPrimitiveArrayType(property))
        {
            PropertyType type = SerializationUtil.GetPropertyPrimitiveType(property);

            //align if this is not a reference field
            if (property.GetCustomAttribute<ReferenceAttribute>() == null)
                AlignForProperty(property, alignment, type);

            if (SerializationUtil.FieldTypeToType(type) != elementType)
                throw new SerializationException("Conversion of array data not supported yet");

            WriteFieldTypeArrayDirect(type, value);
        }
        else if (elementType == typeof(string))
            throw new SerializationException();
        else
        {
            AlignForProperty(property, alignment, PropertyType.U8);
            MethodInfo writeMethod = elementType.GetMethod("Write", new[] { typeof(EndianBinaryWriter) });
            if (writeMethod == null)
                writeMethod = elementType.GetMethod("Write", new[] { typeof(EndianBinaryWriterEx) });
            if (writeMethod != null)
            {
                for (int i = 0; i < size; i++)
                    writeMethod.Invoke(value.GetValue(i), new object[] { this });
            }
            else
                throw new SerializationException();
        }
    }

    //private void WriteVector2dDirect(FieldInfo field, Vector2d value)
    //{
    //    var type     = SerializationUtil.GetVectorPrimitiveType(field);
    //    var trueType = SerializationUtil.FieldTypeToType(type);
    //    WriteFieldTypeDirect(type, SerializationUtil.Cast(value.X, trueType));
    //    WriteFieldTypeDirect(type, SerializationUtil.Cast(value.Y, trueType));
    //}

    //private void WriteVector2<T>(T obj, FieldInfo field, FieldAlignment alignment)
    //{
    //    var type = SerializationUtil.GetVectorPrimitiveType(field);

    //    //align if this is not a reference field
    //    if (field.GetCustomAttribute<ReferenceAttribute>() == null)
    //        AlignForField(field, alignment, type);

    //    WriteVector2dDirect(field, (Vector2d)field.GetValue(obj));
    //}

    //private void WriteVector3dDirect(FieldInfo field, Vector3d value)
    //{
    //    var type     = SerializationUtil.GetVectorPrimitiveType(field);
    //    var trueType = SerializationUtil.FieldTypeToType(type);
    //    WriteFieldTypeDirect(type, SerializationUtil.Cast(value.X, trueType));
    //    WriteFieldTypeDirect(type, SerializationUtil.Cast(value.Y, trueType));
    //    WriteFieldTypeDirect(type, SerializationUtil.Cast(value.Z, trueType));
    //}

    //private void WriteVector3<T>(T obj, FieldInfo field, FieldAlignment alignment)
    //{
    //    var type = SerializationUtil.GetVectorPrimitiveType(field);

    //    //align if this is not a reference field
    //    if (field.GetCustomAttribute<ReferenceAttribute>() == null)
    //        AlignForField(field, alignment, type);

    //    WriteVector3dDirect(field, (Vector3d)field.GetValue(obj));
    //}

    /// <summary>
    /// Writes an object to the stream
    /// </summary>
    /// <param name="obj">The object to write</param>
    /// <typeparam name="T">The type of the object</typeparam>
    /// <exception cref="SerializationException">Thrown if there is an issue serializing the object to binary</exception>
    public void WriteObject<T>(T obj)
    {
        var fields = SerializationUtil.GetPropertiesInOrder<T>();

        PropertyAlignment alignment = SerializationUtil.GetPropertyAlignment<T>();
        foreach (PropertyInfo field in fields)
        {
            var refAttrib = field.GetCustomAttribute<ReferenceAttribute>();
            if (refAttrib != null)
            {
                if (SerializationUtil.HasPrimitiveType(field))
                    throw new SerializationException("Reference field cannot be a primitive type");

                AlignForProperty(field, alignment, refAttrib.PointerPropertyType);

                object refValue = field.GetValue(obj);
                if (refValue != null)
                {
                    if (!_references.ContainsKey(refValue))
                        _references.Add(refValue, new List<(long, Chunk, ReferenceAttribute)>());
                    _references[refValue].Add((BaseStream.Position, _chunks.Count == 0 ? null : _chunks.Peek(),
                        refAttrib));
                }

                Type fieldType = SerializationUtil.FieldTypeToType(refAttrib.PointerPropertyType);
                WriteFieldTypeDirect(refAttrib.PointerPropertyType, Convert.ChangeType(0, fieldType));
                continue;
            }

            //if (field.FieldType == typeof(Vector2d))
            //    WriteVector2(obj, field, alignment);
            //else if (field.FieldType == typeof(Vector3d))
            //    WriteVector3(obj, field, alignment);
            //else 
            if (SerializationUtil.HasPrimitiveType(field))
                WritePrimitive(obj, field, alignment);
            else if (field.PropertyType == typeof(string))
            {
                throw new SerializationException();
            }
            else if (field.PropertyType.IsArray)
                WriteArray(obj, field, alignment);
            else
            {
                object value = field.GetValue(obj);
                if (value == null)
                    throw new SerializationException("Field value is null");

                MethodInfo writeMethod = field.PropertyType.GetMethod("Write", new[] { typeof(EndianBinaryWriter) });
                if (writeMethod == null)
                    writeMethod = field.PropertyType.GetMethod("Write", new[] { typeof(EndianBinaryWriterEx) });
                if (writeMethod != null)
                    writeMethod.Invoke(value, new object[] { this });
                else
                {
                    throw new SerializationException();
                }
            }
        }
    }

    /// <summary>
    /// Indicates the start of a reference object
    /// </summary>
    /// <param name="obj"></param>
    public void StartOfRefObject(object obj)
    {
        _refObjAddresses.Add(obj, BaseStream.Position);
    }

    /// <summary>
    /// Writes all reference objects to the stream
    /// </summary>
    /// <exception cref="Exception">Thrown if there are no reference objects that have been written</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the reference types are invalid</exception>
    public void FlushReferences()
    {
        foreach (var reference in _references)
        {
            if (!_refObjAddresses.ContainsKey(reference.Key))
                throw new("Referenced object has not been written");

            long address = _refObjAddresses[reference.Key];

            foreach ((long address, Chunk chunk, ReferenceAttribute refInfo) pointer in reference.Value)
            {
                long ptr = pointer.refInfo.Type switch
                {
                    ReferenceType.Absolute => address,
                    ReferenceType.ChunkRelative => address - (pointer.chunk?.StartAddress ?? 0),
                    ReferenceType.PropertyRelative => address - pointer.address,
                    _ => throw new ArgumentOutOfRangeException(),
                };

                Type fieldType = SerializationUtil.FieldTypeToType(pointer.refInfo.PointerPropertyType);
                BaseStream.Position = pointer.address;
                WriteFieldTypeDirect(pointer.refInfo.PointerPropertyType, Convert.ChangeType(ptr, fieldType));
            }
        }

        _references.Clear();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!Disposed)
            FlushReferences();

        base.Dispose(disposing);
    }
}