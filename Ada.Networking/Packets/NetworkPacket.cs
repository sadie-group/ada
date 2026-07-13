using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public sealed class NetworkPacket(short packetId, byte[] buffer, int offset, int length)
    : INetworkPacket
{
    public short PacketId { get; } = packetId;
    public ReadOnlyMemory<byte> Data { get; } = new(buffer, offset, length);
}