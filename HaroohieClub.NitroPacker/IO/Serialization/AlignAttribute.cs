using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// An attribute indicating the alignment of the property
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class AlignAttribute : Attribute
{
    /// <summary>
    /// Byte alignment
    /// </summary>
    public int Alignment { get; }

    /// <summary>
    /// Specifies the alignment of the attribute
    /// </summary>
    /// <param name="alignment">The byte-alignment</param>
    public AlignAttribute(int alignment)
    {
        Alignment = alignment;
    }
}