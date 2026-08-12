using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Ada.Networking.Packets.Serialization;

internal static class PropertyAccessorCache
{
    private static readonly ConcurrentDictionary<PropertyInfo, Func<object, object?>> Getters = new();
    private static readonly ConcurrentDictionary<PropertyInfo, Action<object, object?>> Setters = new();

    public static object? GetValue(PropertyInfo property, object target)
        => Getters.GetOrAdd(property, BuildGetter)(target);

    public static void SetValue(PropertyInfo property, object target, object? value)
        => Setters.GetOrAdd(property, BuildSetter)(target, value);

    private static Func<object, object?> BuildGetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var access = Expression.Property(typedInstance, property);
        var boxed = Expression.Convert(access, typeof(object));

        return Expression.Lambda<Func<object, object?>>(boxed, instance).Compile();
    }

    private static Action<object, object?> BuildSetter(PropertyInfo property)
    {
        var instance = Expression.Parameter(typeof(object), "instance");
        var value = Expression.Parameter(typeof(object), "value");
        var typedInstance = Expression.Convert(instance, property.DeclaringType!);
        var typedValue = Expression.Convert(value, property.PropertyType);
        var assign = Expression.Assign(Expression.Property(typedInstance, property), typedValue);

        return Expression.Lambda<Action<object, object?>>(assign, instance, value).Compile();
    }
}
