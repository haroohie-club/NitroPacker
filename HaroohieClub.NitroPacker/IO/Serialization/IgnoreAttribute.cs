using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Indicates that this property should not be serialized
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class IgnoreAttribute : Attribute
{
    /// <summary>
    /// Constructs the attribute
    /// </summary>
    public IgnoreAttribute() { }
}