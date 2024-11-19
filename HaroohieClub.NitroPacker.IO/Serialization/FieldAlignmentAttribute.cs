using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

public enum PropertyAlignment
{
    Packed,
    FieldSize
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class FieldAlignmentAttribute : Attribute
{
    public PropertyAlignment Alignment { get; }

    public FieldAlignmentAttribute(PropertyAlignment alignment)
    {
        Alignment = alignment;
    }
}