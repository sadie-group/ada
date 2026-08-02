using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class DefaultPacketIdMapTests
{
    [PacketId(4242)]
    public class MappedHandler : INetworkPacketEventHandler
    {
        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    [PacketId(4243)]
    public class MappedWriter : AbstractPacketWriter;

    public class UnmappedHandler : INetworkPacketEventHandler
    {
        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    private static DefaultPacketIdMap CreateMap()
        => new(() => [typeof(DefaultPacketIdMapTests).Assembly]);

    [Test]
    public void TryGetHandlerType_AttributedHandler_ResolvesType()
    {
        var map = CreateMap();

        Assert.That(map.TryGetHandlerType(4242, out var handlerType), Is.True);
        Assert.That(handlerType, Is.EqualTo(typeof(MappedHandler)));
    }

    [Test]
    public void TryGetHandlerType_WriterId_IsNotAHandler()
    {
        var map = CreateMap();

        Assert.That(map.TryGetHandlerType(4243, out _), Is.False);
    }

    [Test]
    public void TryGetOutgoingId_AttributedWriter_ResolvesId()
    {
        var map = CreateMap();

        Assert.That(map.TryGetOutgoingId(typeof(MappedWriter), out var id), Is.True);
        Assert.That(id, Is.EqualTo((short)4243));
    }

    [Test]
    public void TryGetOutgoingId_UnattributedType_ReturnsFalse()
    {
        var map = CreateMap();

        Assert.That(map.TryGetOutgoingId(typeof(UnmappedHandler), out _), Is.False);
    }
}
