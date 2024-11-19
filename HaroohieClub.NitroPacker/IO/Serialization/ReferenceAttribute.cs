using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

public enum ReferenceType
{
    Absolute,
    ChunkRelative,
    FieldRelative
}

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class ReferenceAttribute : Attribute
{
    public ReferenceType Type { get; }
    public PropertyType PointerPropertyType { get; }
    public int Offset { get; }

    public ReferenceAttribute(ReferenceType type, PropertyType pointerPropertyType = PropertyType.U32, int offset = 0)
    {
        Type = type;
        PointerPropertyType = pointerPropertyType;
        Offset = offset;
    }
}