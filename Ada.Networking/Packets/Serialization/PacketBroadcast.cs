using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets.Serialization;

public static class PacketBroadcast
{
    public static void Queue(AbstractPacketWriter writer, IEnumerable<INetworkObject> recipients)
    {
        IPacketCodec? firstCodec = null;
        INetworkPacketWriter? firstPacket = null;
        Dictionary<IPacketCodec, INetworkPacketWriter>? others = null;

        foreach (var recipient in recipients)
        {
            recipient.QueueOutbound(
                ResolvePacket(writer, recipient.Codec, ref firstCodec, ref firstPacket, ref others));
        }
    }

    public static void SendAndFlush(AbstractPacketWriter writer, IEnumerable<INetworkObject> recipients)
    {
        IPacketCodec? firstCodec = null;
        INetworkPacketWriter? firstPacket = null;
        Dictionary<IPacketCodec, INetworkPacketWriter>? others = null;

        List<INetworkObject>? queued = null;

        foreach (var recipient in recipients)
        {
            recipient.QueueOutbound(
                ResolvePacket(writer, recipient.Codec, ref firstCodec, ref firstPacket, ref others));

            (queued ??= []).Add(recipient);
        }

        if (queued == null)
        {
            return;
        }

        foreach (var recipient in queued)
        {
            recipient.FlushAsync();
        }
    }

    private static INetworkPacketWriter ResolvePacket(
        AbstractPacketWriter writer,
        IPacketCodec codec,
        ref IPacketCodec? firstCodec,
        ref INetworkPacketWriter? firstPacket,
        ref Dictionary<IPacketCodec, INetworkPacketWriter>? others)
    {
        if (firstCodec == null)
        {
            firstCodec = codec;
            firstPacket = NetworkPacketWriterSerializer.Serialize(writer, codec);

            return firstPacket;
        }

        if (ReferenceEquals(codec, firstCodec))
        {
            return firstPacket!;
        }

        others ??= [];

        if (!others.TryGetValue(codec, out var packet))
        {
            packet = NetworkPacketWriterSerializer.Serialize(writer, codec);
            others[codec] = packet;
        }

        return packet;
    }
}
