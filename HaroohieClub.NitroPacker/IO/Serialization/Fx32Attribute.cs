using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Indicates that this property should be serialized as an fx32 fixed-point integer
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class Fx32Attribute : Attribute
{
    /// <summary>
    /// Constructs the property
    /// </summary>
    public Fx32Attribute() { }
}