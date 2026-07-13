namespace Ada.API.Interfaces.Networking.Packets;

public interface INetworkPacket
{
    short PacketId { get; }
    ReadOnlyMemory<byte> Data { get; }
}