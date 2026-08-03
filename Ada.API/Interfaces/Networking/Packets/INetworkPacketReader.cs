namespace Ada.API.Interfaces.Networking.Packets;

public interface INetworkPacketReader
{
    int Remaining { get; }
    string ReadString();
    int ReadInt();
    short ReadShort();
    bool ReadBool();
    long ReadLong();
    byte ReadByte();
}
