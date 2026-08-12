namespace Ada.API;

public static class PacketWriterFastPath
{
    public static Func<object, INetworkPacketWriter, bool>? Handler { get; set; }
}
