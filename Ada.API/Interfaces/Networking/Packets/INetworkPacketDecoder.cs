namespace Ada.API.Interfaces.Networking.Packets;

public interface INetworkPacketDecoder
{
    INetworkPacket Decode(Guid guid, byte[] buffer, int length);
}