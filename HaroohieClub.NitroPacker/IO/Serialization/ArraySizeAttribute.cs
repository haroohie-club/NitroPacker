using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Attribute indicating the size of an array for binary serialization
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ArraySizeAttribute : Attribute
{
    /// <summary>
    /// The fixed size of an array
    /// </summary>
    public int FixedSize { get; }

    /// <summary>
    /// Specifies 
    /// </summary>
    /// <param name="fixedSize"></param>
    public ArraySizeAttribute(int fixedSize)
    {
        FixedSize = fixedSize;
    }
}