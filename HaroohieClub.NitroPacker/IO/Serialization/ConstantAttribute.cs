using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

/// <summary>
/// Attribute for constants that should be serialized
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class ConstantAttribute : Attribute
{
    /// <summary>
    /// The value of the constant
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Establishes a constant with a value
    /// </summary>
    /// <param name="value">The value of the constant</param>
    public ConstantAttribute(object value)
    {
        Value = value;
    }
}