namespace Ada.API.Interfaces;

public interface INetworkPacketProcessor
{
    void ProcessPacket(Guid guid, byte[] data);
}