using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// The reference type of the property
/// </summary>
public enum ReferenceType
{
    /// <summary>
    /// Absolute references
    /// </summary>
    Absolute,
    /// <summary>
    /// Relative to a given chunk
    /// </summary>
    ChunkRelative,
    /// <summary>
    /// Relative to the properties
    /// </summary>
    PropertyRelative,
}

/// <summary>
/// Defines a reference property
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ReferenceAttribute : Attribute
{
    /// <summary>
    /// The reference type
    /// </summary>
    public ReferenceType Type { get; }
    /// <summary>
    /// The tye of property pointed to
    /// </summary>
    public PropertyType PointerPropertyType { get; }
    /// <summary>
    /// The offset
    /// </summary>
    public int Offset { get; }

    /// <summary>
    /// Constructs the attribute
    /// </summary>
    /// <param name="type">The type</param>
    /// <param name="pointerPropertyType">The type pointed to</param>
    /// <param name="offset">The offset</param>
    public ReferenceAttribute(ReferenceType type, PropertyType pointerPropertyType = PropertyType.U32, int offset = 0)
    {
        Type = type;
        PointerPropertyType = pointerPropertyType;
        Offset = offset;
    }
}