using Ada.API;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public sealed class BinaryPacketCodec(IPacketIdMap idMap, INetworkPacketDecoder decoder) : IPacketCodec
{
    public const string RevisionName = "PRODUCTION";

    public string Revision => RevisionName;

    public IPacketIdMap IdMap { get; } = idMap;

    public INetworkPacketDecoder Decoder { get; } = decoder;

    public INetworkPacketWriter CreateWriter() => new NetworkPacketWriter();

    public INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body) => new NetworkPacketReader(body);
}
