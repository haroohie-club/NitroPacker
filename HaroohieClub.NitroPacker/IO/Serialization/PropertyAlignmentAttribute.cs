using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Alignment of properties
/// </summary>
public enum PropertyAlignment
{
    /// <summary>
    /// The properties should be packed together (not aligned)
    /// </summary>
    Packed,
    /// <summary>
    /// The properties should be aligned to the field size
    /// </summary>
    FieldSize,
}

/// <summary>
/// Indicates the alignment of this class or struct
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
public sealed class PropertyAlignmentAttribute : Attribute
{
    /// <summary>
    /// The alignment of the class or struct
    /// </summary>
    public PropertyAlignment Alignment { get; }

    /// <summary>
    /// Constructs the property
    /// </summary>
    /// <param name="alignment">The alignment of properties in this class or struct</param>
    public PropertyAlignmentAttribute(PropertyAlignment alignment)
    {
        Alignment = alignment;
    }
}