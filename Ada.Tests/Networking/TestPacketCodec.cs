using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Packets;

namespace Ada.Tests.Networking;

public static class TestPacketCodec
{
    public static IPacketCodec Instance { get; } =
        new BinaryPacketCodec(new DefaultPacketIdMap(), new NetworkPacketDecoder());
}
