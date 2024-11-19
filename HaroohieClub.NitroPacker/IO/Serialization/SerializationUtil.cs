using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HaroohieClub.NitroPacker.IO.Serialization;

public static class SerializationUtil
{
    public static PropertyType TypeToPropertyType(Type type)
    {
        if (type == typeof(byte))
            return PropertyType.U8;
        if (type == typeof(sbyte))
            return PropertyType.S8;
        if (type == typeof(ushort))
            return PropertyType.U16;
        if (type == typeof(short))
            return PropertyType.S16;
        if (type == typeof(uint))
            return PropertyType.U32;
        if (type == typeof(int))
            return PropertyType.S32;
        if (type == typeof(ulong))
            return PropertyType.U64;
        if (type == typeof(long))
            return PropertyType.S64;

        throw new("Unexpected primitive field type " + type.Name);
    }

    public static Type FieldTypeToType(PropertyType type) => type switch
    {
        PropertyType.U8 => typeof(byte),
        PropertyType.S8 => typeof(sbyte),
        PropertyType.U16 => typeof(ushort),
        PropertyType.S16 => typeof(short),
        PropertyType.U32 => typeof(uint),
        PropertyType.S32 => typeof(int),
        PropertyType.U64 => typeof(ulong),
        PropertyType.S64 => typeof(long),
        PropertyType.Fx16 => typeof(double),
        PropertyType.Fx32 => typeof(double),
        PropertyType.Float => typeof(float),
        PropertyType.Double => typeof(double),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static int GetTypeSize(PropertyType type) => type switch
    {
        PropertyType.U8 => 1,
        PropertyType.S8 => 1,
        PropertyType.U16 => 2,
        PropertyType.S16 => 2,
        PropertyType.U32 => 4,
        PropertyType.S32 => 4,
        PropertyType.U64 => 8,
        PropertyType.S64 => 8,
        PropertyType.Fx16 => 2,
        PropertyType.Fx32 => 4,
        PropertyType.Float => 4,
        PropertyType.Double => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static bool HasPrimitiveType(PropertyInfo property)
    {
        return property.PropertyType.IsPrimitive ||
               property.PropertyType.IsEnum ||
               property.GetCustomAttribute<TypeAttribute>() != null ||
               property.GetCustomAttribute<Fx32Attribute>() != null ||
               property.GetCustomAttribute<Fx16Attribute>() != null;
    }

    public static bool HasPrimitiveArrayType(PropertyInfo property)
    {
        if (!property.PropertyType.IsArray)
            return false;

        Type type = property.PropertyType.GetElementType();

        return type.IsPrimitive ||
               property.GetCustomAttribute<TypeAttribute>() != null ||
               property.GetCustomAttribute<Fx32Attribute>() != null ||
               property.GetCustomAttribute<Fx16Attribute>() != null;
    }

    public static PropertyType GetPropertyPrimitiveType(PropertyInfo property)
    {
        bool isFx32 = property.GetCustomAttribute<Fx32Attribute>() != null;
        bool isFx16 = property.GetCustomAttribute<Fx16Attribute>() != null;
        bool hasFieldType = property.GetCustomAttribute<TypeAttribute>() != null;
        int count = (isFx32 ? 1 : 0) + (isFx16 ? 1 : 0) + (hasFieldType ? 1 : 0);
        if (count > 1)
            throw new("More than one property type specified for property " + property.Name + " in type " +
                      property.DeclaringType?.Name);

        if (isFx32)
            return PropertyType.Fx32;
        if (isFx16)
            return PropertyType.Fx16;
        if (hasFieldType)
            return property.GetCustomAttribute<TypeAttribute>().Type;
        if (property.PropertyType.IsArray)
        {
            Type elemType = property.PropertyType.GetElementType();
            if (elemType.IsEnum)
                return TypeToPropertyType(elemType.GetEnumUnderlyingType());
            return TypeToPropertyType(elemType);
        }

        if (property.PropertyType.IsEnum)
            return TypeToPropertyType(property.PropertyType.GetEnumUnderlyingType());

        return TypeToPropertyType(property.PropertyType);
    }

    public static PropertyType GetVectorPrimitiveType(PropertyInfo property)
    {
        bool isFx32 = property.GetCustomAttribute<Fx32Attribute>() != null;
        bool isFx16 = property.GetCustomAttribute<Fx16Attribute>() != null;
        bool hasPropertyType = property.GetCustomAttribute<TypeAttribute>() != null;
        int count = (isFx32 ? 1 : 0) + (isFx16 ? 1 : 0) + (hasPropertyType ? 1 : 0);
        if (count > 1)
            throw new("More than one property type specified for property " + property.Name + " in type " +
                      property.DeclaringType?.Name);

        if (isFx32)
            return PropertyType.Fx32;
        if (isFx16)
            return PropertyType.Fx16;
        if (hasPropertyType)
            return property.GetCustomAttribute<TypeAttribute>().Type;
        return PropertyType.Float;
    }

    public static IEnumerable<PropertyInfo> GetPropertiesInOrder<T>()
        => GetPropertiesInOrder(typeof(T));

    public static IEnumerable<PropertyInfo> GetPropertiesInOrder(Type type)
    {
        //Sorting by MetadataToken works, but may not be future-proof
        return type.GetProperties()
            .Where(f => f.GetCustomAttribute<IgnoreAttribute>() == null)
            .OrderBy(f => f.MetadataToken);
    }

    public static PropertyAlignment GetPropertyAlignment<T>()
        => GetPropertyAlignment(typeof(T));

    public static PropertyAlignment GetPropertyAlignment(Type type)
        => type.GetCustomAttribute<PropertyAlignmentAttribute>()?.Alignment ?? PropertyAlignment.Packed;

    private static readonly ConcurrentDictionary<(Type, Type), Delegate> CastCache = new();

    public static T Cast<T>(object data)
        => (T)Cast(data, typeof(T));

    public static object Cast(object data, Type type)
    {
        Type inType = data.GetType();

        if (inType == type)
            return data;

        if (CastCache.TryGetValue((inType, type), out Delegate func))
            return func.DynamicInvoke(data);

        ParameterExpression dataParam = Expression.Parameter(data.GetType());
        Delegate run = Expression.Lambda(Expression.Convert(dataParam, type), dataParam).Compile();

        CastCache.TryAdd((inType, type), run);

        return run.DynamicInvoke(data);
    }
}