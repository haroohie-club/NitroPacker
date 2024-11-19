using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using HaroohieClub.NitroPacker.IO.Serialization;

namespace HaroohieClub.NitroPacker.IO;

public class EndianBinaryReaderEx : EndianBinaryReader
{
    public EndianBinaryReaderEx(Stream baseStream)
        : base(baseStream) { }

    public EndianBinaryReaderEx(Stream baseStream, Endianness endianness)
        : base(baseStream, endianness) { }

    public void ReadPadding(int alignment)
    {
        if (BaseStream.Position % alignment == 0)
            return;
        BaseStream.Position += alignment - BaseStream.Position % alignment;
    }

    private Stack<long> Chunks = new();

    public void BeginChunk()
    {
        Chunks.Push(BaseStream.Position);
    }

    public void EndChunk()
    {
        Chunks.Pop();
    }

    public void EndChunk(long sectionSize)
    {
        JumpRelative(sectionSize);
        Chunks.Pop();
    }

    public long JumpRelative(long offset)
    {
        long curPos = BaseStream.Position;
        BaseStream.Position = GetChunkRelativePointer(offset);
        return curPos;
    }

    private long GetChunkRelativePointer(long offset)
    {
        if (Chunks.Count == 0)
            return offset;
        return Chunks.Peek() + offset;
    }

    public long GetRelativeOffset()
    {
        if (Chunks.Count == 0)
            return BaseStream.Position;

        return BaseStream.Position - Chunks.Peek();
    }

    public uint ReadSignature(uint expected)
    {
        uint signature = Read<uint>();
        if (signature != expected)
            throw new SignatureNotCorrectException(signature, expected, BaseStream.Position - 4);
        return signature;
    }

    public T ReadObject<T>() where T : new()
    {
        T result = new T();
        ReadObject(result);
        return result;
    }

    private object ReadFieldTypeDirect(PropertyType type) => type switch
    {
        //The object cast is needed once to ensure the switch doesn't cast to double
        PropertyType.U8 => Read<byte>(),
        PropertyType.S8 => Read<sbyte>(),
        PropertyType.U16 => Read<ushort>(),
        PropertyType.S16 => Read<short>(),
        PropertyType.U32 => Read<uint>(),
        PropertyType.S32 => Read<int>(),
        PropertyType.U64 => Read<ulong>(),
        PropertyType.S64 => Read<long>(),
        PropertyType.Fx16 => ReadFx16(),
        PropertyType.Fx32 => ReadFx32(),
        PropertyType.Float => Read<float>(),
        PropertyType.Double => Read<double>(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private Array ReadFieldTypeArrayDirect(PropertyType type, int count) => type switch
    {
        PropertyType.U8 => Read<byte>(count),
        PropertyType.S8 => Read<sbyte>(count),
        PropertyType.U16 => Read<ushort>(count),
        PropertyType.S16 => Read<short>(count),
        PropertyType.U32 => Read<uint>(count),
        PropertyType.S32 => Read<int>(count),
        PropertyType.U64 => Read<ulong>(count),
        PropertyType.S64 => Read<long>(count),
        PropertyType.Fx16 => ReadFx16s(count),
        PropertyType.Fx32 => ReadFx32s(count),
        PropertyType.Float => Read<float>(count),
        PropertyType.Double => Read<double>(count),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    private void AlignForProperty(PropertyInfo property, PropertyAlignment alignment, PropertyType type)
    {
        var alignAttribute = property.GetCustomAttribute<AlignAttribute>();
        if (alignAttribute != null)
            ReadPadding(alignAttribute.Alignment);
        else if (alignment == PropertyAlignment.FieldSize)
            ReadPadding(SerializationUtil.GetTypeSize(type));
    }

    private void ReadPrimitive<T>(T target, PropertyInfo property, PropertyAlignment alignment)
    {
        PropertyType type = SerializationUtil.GetPropertyPrimitiveType(property);

        //align if this is not a reference field
        if (property.GetCustomAttribute<ReferenceAttribute>() == null)
            AlignForProperty(property, alignment, type);

        long address = BaseStream.Position;

        object value = ReadFieldTypeDirect(type);
        object finalValue;
        if (property.PropertyType == typeof(bool))
            finalValue = Convert.ChangeType(value, typeof(bool));
        else
            finalValue = SerializationUtil.Cast(value, property.PropertyType);

        property.SetValue(target, finalValue);

        var constAttrib = property.GetCustomAttribute<ConstantAttribute>();
        if (constAttrib != null && !constAttrib.Value.Equals(finalValue))
            throw new InvalidDataException(
                $"Const field \"{property.Name}\" in \"{typeof(T).Name}\" at address 0x{address:X} has an invalid value. Got: {finalValue:X}, expected: {constAttrib.Value:X}");
    }

    private void ReadArray<T>(T target, PropertyInfo property, PropertyAlignment alignment)
    {
        //align if this is not a reference field
        if (property.GetCustomAttribute<ReferenceAttribute>() == null)
        {
            var alignAttr = property.GetCustomAttribute<AlignAttribute>();
            if (alignAttr != null)
                ReadPadding(alignAttr.Alignment);
        }

        var arrSizeAttr = property.GetCustomAttribute<ArraySizeAttribute>();
        if (arrSizeAttr == null)
            throw new SerializationException(
                $"No array size attribute found for field \"{property.Name}\" in \"{typeof(T).Name}\"");
        int size = -1;
        if (arrSizeAttr.SizeField != null)
        {
            FieldInfo sizeField = typeof(T).GetField(arrSizeAttr.SizeField);
            if (sizeField == null)
                throw new SerializationException(
                    $"Array size field \"{arrSizeAttr.SizeField}\" not found in \"{typeof(T).Name}\"");
            size = (int)Convert.ChangeType(sizeField.GetValue(target), typeof(int));
        }
        else
            size = arrSizeAttr.FixedSize;

        Type elementType = property.PropertyType.GetElementType();

        Array value;

        // Not needed for this
        //if (elementType == typeof(Vector2d))
        //{
        //    value = new Vector2d[size];
        //    for (int i = 0; i < size; i++)
        //        value.SetValue(ReadVector2dDirect(field), i);
        //}
        //else if (elementType == typeof(Vector3d))
        //{
        //    value = new Vector3d[size];
        //    for (int i = 0; i < size; i++)
        //        value.SetValue(ReadVector3dDirect(field), i);
        //}
        //else 
        if (SerializationUtil.HasPrimitiveArrayType(property))
        {
            PropertyType type = SerializationUtil.GetPropertyPrimitiveType(property);

            //align if this is not a reference field
            if (property.GetCustomAttribute<ReferenceAttribute>() == null)
                AlignForProperty(property, alignment, type);

            value = ReadFieldTypeArrayDirect(type, size);

            if (SerializationUtil.FieldTypeToType(type) != elementType)
                throw new SerializationException("Conversion of array data not supported yet");
        }
        else if (elementType == typeof(string))
            throw new SerializationException();
        else
        {
            ConstructorInfo readerConstructor = elementType.GetConstructor(new[] { typeof(EndianBinaryReader) });
            if (readerConstructor == null)
                readerConstructor = elementType.GetConstructor(new[] { typeof(EndianBinaryReaderEx) });
            if (readerConstructor != null)
            {
                value = Array.CreateInstance(elementType, size);
                for (int i = 0; i < size; i++)
                    value.SetValue(readerConstructor.Invoke(new object[] { this }), i);
            }
            else
                throw new SerializationException();
        }

        property.SetValue(target, value);
    }

    //private Vector2d ReadVector2dDirect(FieldInfo field)
    //{
    //    var type       = SerializationUtil.GetVectorPrimitiveType(field);
    //    var components = new double[2];
    //    for (int i = 0; i < 2; i++)
    //    {
    //        object value = ReadFieldTypeDirect(type);
    //        components[i] = (double)SerializationUtil.Cast(value, typeof(double));
    //    }

    //    return new Vector2d(components[0], components[1]);
    //}

    //private void ReadVector2<T>(T target, FieldInfo field, FieldAlignment alignment)
    //{
    //    var type = SerializationUtil.GetVectorPrimitiveType(field);

    //    //align if this is not a reference field
    //    if (field.GetCustomAttribute<ReferenceAttribute>() == null)
    //        AlignForField(field, alignment, type);

    //    field.SetValue(target, ReadVector2dDirect(field));
    //}

    //private Vector3d ReadVector3dDirect(FieldInfo field)
    //{
    //    var type       = SerializationUtil.GetVectorPrimitiveType(field);
    //    var components = new double[3];
    //    for (int i = 0; i < 3; i++)
    //    {
    //        object value = ReadFieldTypeDirect(type);
    //        components[i] = (double)SerializationUtil.Cast(value, typeof(double));
    //    }

    //    return new Vector3d(components[0], components[1], components[2]);
    //}

    //private void ReadVector3<T>(T target, FieldInfo field, FieldAlignment alignment)
    //{
    //    var type = SerializationUtil.GetVectorPrimitiveType(field);

    //    //align if this is not a reference field
    //    if (field.GetCustomAttribute<ReferenceAttribute>() == null)
    //        AlignForField(field, alignment, type);

    //    field.SetValue(target, ReadVector3dDirect(field));
    //}

    public void ReadObject<T>(T target)
    {
        var properties = SerializationUtil.GetPropertiesInOrder<T>();

        PropertyAlignment alignment = SerializationUtil.GetFieldAlignment<T>();
        foreach (PropertyInfo property in properties)
        {
            long curPos = BaseStream.Position;
            var refAttrib = property.GetCustomAttribute<ReferenceAttribute>();
            if (refAttrib != null)
            {
                if (SerializationUtil.HasPrimitiveType(property))
                    throw new("Reference field cannot be a primitive type");

                AlignForProperty(property, alignment, refAttrib.PointerPropertyType);
                long address = BaseStream.Position;
                object val = ReadFieldTypeDirect(refAttrib.PointerPropertyType);
                curPos = BaseStream.Position;
                long ptr = (long)Convert.ChangeType(val, typeof(long));
                switch (refAttrib.Type)
                {
                    case ReferenceType.Absolute:
                        break;
                    case ReferenceType.ChunkRelative:
                        ptr = GetChunkRelativePointer(ptr);
                        break;
                    case ReferenceType.FieldRelative:
                        ptr += address;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                BaseStream.Position = ptr;
            }


            //if (field.FieldType == typeof(Vector2d))
            //    ReadVector2(target, field, alignment);
            //else if (field.FieldType == typeof(Vector3d))
            //    ReadVector3(target, field, alignment);
            //else 
            if (SerializationUtil.HasPrimitiveType(property))
                ReadPrimitive(target, property, alignment);
            else if (property.PropertyType == typeof(string))
            {
                throw new SerializationException();
            }
            else if (property.PropertyType.IsArray)
                ReadArray(target, property, alignment);
            else
            {
                AlignForProperty(property, alignment, PropertyType.U8);
                ConstructorInfo readerConstructor = property.PropertyType.GetConstructor(new[] { typeof(EndianBinaryReader) });
                if (readerConstructor == null)
                    readerConstructor = property.PropertyType.GetConstructor(new[] { typeof(EndianBinaryReaderEx) });
                if (readerConstructor != null)
                {
                    object obj = readerConstructor.Invoke(new object[] { this });
                    property.SetValue(target, obj);
                }
                else
                    throw new SerializationException();
            }

            if (refAttrib != null)
                BaseStream.Position = curPos;
        }
    }
}