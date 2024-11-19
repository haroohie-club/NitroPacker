using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

public enum PropertyAlignment
{
    Packed,
    FieldSize
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class PropertyAlignmentAttribute : Attribute
{
    public PropertyAlignment Alignment { get; }

    public PropertyAlignmentAttribute(PropertyAlignment alignment)
    {
        Alignment = alignment;
    }
}