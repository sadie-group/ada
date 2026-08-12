namespace Ada.API;

public interface INetworkPacketWriter
{
    void WriteString(string data);
    void WriteShort(short data);
    void WriteInteger(int data);
    void WriteLong(long data);
    void WriteBool(bool boolean);
    void WriteByte(byte b);
    int FramedLength { get; }
    void WriteFramedTo(Span<byte> destination);
    byte[] GetAllBytes();
}