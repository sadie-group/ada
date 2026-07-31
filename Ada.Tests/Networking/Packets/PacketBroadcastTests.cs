using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Packets;
using Ada.Networking.Packets.Serialization;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketBroadcastTests
{
    [PacketId(1)]
    private class BroadcastWriter : AbstractPacketWriter
    {
        public required int Value { get; init; }
    }

    private sealed class FixedIdMap(short outgoingId) : IPacketIdMap
    {
        public bool TryGetHandlerType(short packetId, out Type? handlerType)
        {
            handlerType = null;
            return false;
        }

        public bool TryGetOutgoingId(Type writerType, out short packetId)
        {
            packetId = outgoingId;
            return true;
        }
    }

    private sealed class CountingCodec(short outgoingId, string revision) : IPacketCodec
    {
        public int WritersCreated { get; private set; }

        public string Revision { get; } = revision;

        public IPacketIdMap IdMap { get; } = new FixedIdMap(outgoingId);

        public INetworkPacketDecoder Decoder { get; } = new NetworkPacketDecoder();

        public INetworkPacketWriter CreateWriter()
        {
            WritersCreated++;
            return new NetworkPacketWriter();
        }

        public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
    }

    private sealed class RecordingRecipient(IPacketCodec codec) : INetworkObject
    {
        public List<INetworkPacketWriter> Queued { get; } = [];

        public IPacketCodec Codec { get; set; } = codec;

        public Task WriteToStreamAsync(AbstractPacketWriter writer) => Task.CompletedTask;

        public Task WriteToStreamAsync(INetworkPacketWriter writer)
        {
            Queued.Add(writer);
            return Task.CompletedTask;
        }

        public void QueueOutbound(INetworkPacketWriter writer) => Queued.Add(writer);

        public Task FlushAsync() => Task.CompletedTask;

        public System.Net.IPAddress IpAddress { get; set; } = System.Net.IPAddress.Loopback;

        public Guid Guid { get; set; } = Guid.NewGuid();

        public System.Net.WebSockets.WebSocket WebSocket { get; set; } = null!;
    }

    [Test]
    public void Queue_AllRecipientsShareCodec_SerializesOnce()
    {
        var codec = new CountingCodec(1, "PRODUCTION");
        var recipients = new[] { new RecordingRecipient(codec), new RecordingRecipient(codec) };

        PacketBroadcast.Queue(new BroadcastWriter { Value = 7 }, recipients);

        Assert.That(codec.WritersCreated, Is.EqualTo(1));
        Assert.That(recipients[0].Queued.Single(), Is.SameAs(recipients[1].Queued.Single()));
    }

    [Test]
    public void Queue_MixedCodecs_SerializesOncePerCodec()
    {
        var production = new CountingCodec(1, "PRODUCTION");
        var legacy = new CountingCodec(2, "R63B");

        var recipients = new[]
        {
            new RecordingRecipient(production),
            new RecordingRecipient(legacy),
            new RecordingRecipient(production),
            new RecordingRecipient(legacy)
        };

        PacketBroadcast.Queue(new BroadcastWriter { Value = 7 }, recipients);

        Assert.That(production.WritersCreated, Is.EqualTo(1));
        Assert.That(legacy.WritersCreated, Is.EqualTo(1));

        Assert.That(recipients[0].Queued.Single(), Is.SameAs(recipients[2].Queued.Single()));
        Assert.That(recipients[1].Queued.Single(), Is.SameAs(recipients[3].Queued.Single()));
        Assert.That(recipients[0].Queued.Single(), Is.Not.SameAs(recipients[1].Queued.Single()));
    }

    [Test]
    public void Queue_MixedCodecs_EachRecipientGetsItsOwnRevisionHeader()
    {
        var production = new CountingCodec(1, "PRODUCTION");
        var legacy = new CountingCodec(2, "R63B");

        var first = new RecordingRecipient(production);
        var second = new RecordingRecipient(legacy);

        PacketBroadcast.Queue(new BroadcastWriter { Value = 7 }, new[] { first, second });

        Assert.That(HeaderOf(first.Queued.Single()), Is.EqualTo((short)1));
        Assert.That(HeaderOf(second.Queued.Single()), Is.EqualTo((short)2));
    }

    [Test]
    public void Queue_NoRecipients_DoesNotSerialize()
    {
        var codec = new CountingCodec(1, "PRODUCTION");

        PacketBroadcast.Queue(new BroadcastWriter { Value = 7 }, Array.Empty<INetworkObject>());

        Assert.That(codec.WritersCreated, Is.Zero);
    }

    [Test]
    public async Task SendAsync_MixedCodecs_SerializesOncePerCodec()
    {
        var production = new CountingCodec(1, "PRODUCTION");
        var legacy = new CountingCodec(2, "R63B");

        var recipients = new[]
        {
            new RecordingRecipient(production),
            new RecordingRecipient(legacy),
            new RecordingRecipient(production)
        };

        await PacketBroadcast.SendAsync(new BroadcastWriter { Value = 7 }, recipients);

        Assert.That(production.WritersCreated, Is.EqualTo(1));
        Assert.That(legacy.WritersCreated, Is.EqualTo(1));
    }

    private static short HeaderOf(INetworkPacketWriter writer)
    {
        var bytes = writer.GetAllBytes();
        return System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(4));
    }
}
