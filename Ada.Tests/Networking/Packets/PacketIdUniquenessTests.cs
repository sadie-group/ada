using System.Reflection;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events;
using Ada.Networking.Writers.Handshake;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketIdUniquenessTests
{
    private static IEnumerable<(string Name, short Value)> Constants(Type type)
        => type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(short))
            .Select(f => (f.Name, (short) f.GetRawConstantValue()!));

    [Test]
    public void EventHandlerIds_AreUnique()
    {
        var duplicates = Constants(typeof(EventHandlerId))
            .GroupBy(x => x.Value)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Name))}")
            .ToList();

        Assert.That(duplicates, Is.Empty,
            "Two incoming packet ids with the same value silently overwrite each other in the " +
            "handler map, so one of the handlers never runs:\n" + string.Join("\n", duplicates));
    }

    [Test]
    public void HandlerTypes_DoNotShareAPacketId()
    {
        var handlers = typeof(EventHandlerId).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        typeof(INetworkPacketEventHandler).IsAssignableFrom(t))
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<PacketIdAttribute>(false)))
            .Where(x => x.Attribute != null)
            .GroupBy(x => x.Attribute!.Id)
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.Type.Name))}")
            .ToList();

        Assert.That(handlers, Is.Empty,
            "These handlers claim the same incoming packet id, so only one of them is reachable:\n" +
            string.Join("\n", handlers));
    }

    [Test]
    public void WriterTypes_MayShareAnId_ButEachTypeMapsToExactlyOne()
    {
        var writers = typeof(SecureLoginWriter).Assembly
            .GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } &&
                        typeof(AbstractPacketWriter).IsAssignableFrom(t))
            .Select(t => (Type: t, Attributes: t.GetCustomAttributes<PacketIdAttribute>(false).ToList()))
            .Where(x => x.Attributes.Count > 1)
            .Select(x => x.Type.Name)
            .ToList();

        Assert.That(writers, Is.Empty);
    }
}
