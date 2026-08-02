namespace Ada.API.Interfaces.Networking.Packets;

public interface IPacketCodec
{
    string Revision { get; }
    IPacketIdMap IdMap { get; }
    INetworkPacketDecoder Decoder { get; }
    INetworkPacketWriter CreateWriter();
    INetworkPacketReader CreateReader(ReadOnlyMemory<byte> body);
}
