using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Indicates that this property should be serialized as an fx16 fixed-point integer
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class Fx16Attribute : Attribute
{
    /// <summary>
    /// Constructs the attribute
    /// </summary>
    public Fx16Attribute() { }
}