using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// The property type
/// </summary>
public enum PropertyType
{
    /// <summary>
    /// Unsigned 8-bit integer
    /// </summary>
    U8,
    /// <summary>
    /// Signed 8-bit integer
    /// </summary>
    S8,
    /// <summary>
    /// Unsigned 16-bit integer
    /// </summary>
    U16,
    /// <summary>
    /// Signed 16-bit integer
    /// </summary>
    S16,
    /// <summary>
    /// Unsigned 32-bit integer
    /// </summary>
    U32,
    /// <summary>
    /// Signed 32-bit integer
    /// </summary>
    S32,
    /// <summary>
    /// Unsigned 64-bit integer
    /// </summary>
    U64,
    /// <summary>
    /// Signed 64-bit integer
    /// </summary>
    S64,
    /// <summary>
    /// 16-bit fixed point value
    /// </summary>
    Fx16,
    /// <summary>
    /// 32-bit fixed point value
    /// </summary>
    Fx32,
    /// <summary>
    /// Float
    /// </summary>
    Float,
    /// <summary>
    /// Double
    /// </summary>
    Double,
}

/// <summary>
/// Marks the type of this property
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class TypeAttribute : Attribute
{
    /// <summary>
    /// The type
    /// </summary>
    public PropertyType Type { get; }

    /// <summary>
    ///  Constructs the attribute
    /// </summary>
    /// <param name="type">The type</param>
    public TypeAttribute(PropertyType type)
    {
        Type = type;
    }
}