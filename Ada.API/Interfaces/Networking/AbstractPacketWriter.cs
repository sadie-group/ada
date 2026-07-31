using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable CollectionNeverQueried.Global

namespace Ada.API.Interfaces.Networking;

public abstract class AbstractPacketWriter
{
    public Dictionary<string, Action<INetworkPacketWriter>>? InsteadRulesSerialize { get; private set; }
    public Dictionary<string, Action<INetworkPacketWriter>>? AfterRulesSerialize { get; private set; }
    public Dictionary<string, KeyValuePair<Type, Func<object, object>>>? ConversionRules { get; private set; }

    public virtual void OnConfigureRules()
    {
    }

    public void ResetRules()
    {
        InsteadRulesSerialize?.Clear();
        AfterRulesSerialize?.Clear();
        ConversionRules?.Clear();
    }

    public virtual void OnSerialize(INetworkPacketWriter writer)
    {
    }

    protected void Override(string propertyName, Action<INetworkPacketWriter> function)
    {
        (InsteadRulesSerialize ??= new()).Add(propertyName, function);
    }

    protected void After(string propertyName, Action<INetworkPacketWriter> function)
    {
        (AfterRulesSerialize ??= new()).Add(propertyName, function);
    }

    protected void Convert<TType>(string propertyName, Func<object, object> conversion)
    {
        (ConversionRules ??= new()).Add(propertyName, new KeyValuePair<Type, Func<object, object>>(typeof(TType), conversion));
    }

    protected void Override(PropertyInfo propertyInfo, Action<INetworkPacketWriter> function)
    {
        Override(propertyInfo.Name, function);
    }

    protected void After(PropertyInfo propertyInfo, Action<INetworkPacketWriter> function)
    {
        After(propertyInfo.Name, function);
    }

    protected void Convert<TType>(PropertyInfo propertyInfo, Func<object, object> conversion)
    {
        Convert<TType>(propertyInfo.Name, conversion);
    }
}
