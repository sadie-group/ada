namespace Ada.API.Interfaces.Networking.Packets;

public interface INetworkPacketReader
{
    string ReadString();
    int ReadInt();
    short ReadShort();
    bool ReadBool();
    long ReadLong();
    byte ReadByte();
}
