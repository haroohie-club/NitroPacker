using System;

namespace HaroohieClub.NitroPacker.IO.Serialization;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public sealed class IgnoreAttribute : Attribute
{
    public IgnoreAttribute() { }
}