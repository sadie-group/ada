using System.Buffers.Binary;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public class NetworkPacketDecoder : INetworkPacketDecoder
{
    private const int _headerLength = 6;

    public INetworkPacket Decode(Guid guid, byte[] buffer, int length)
    {
        if (length < _headerLength)
        {
            throw new MalformedPacketException(
                $"Frame of {length} byte(s) is shorter than the {_headerLength}-byte packet header.");
        }

        var span = buffer.AsSpan(0, length);

        var offset = 0;

        _ = BinaryPrimitives.ReadInt32BigEndian(span.Slice(offset, 4));
        offset += 4;

        var packetId = BinaryPrimitives.ReadInt16BigEndian(span.Slice(offset, 2));
        offset += 2;

        return new NetworkPacket(packetId, buffer, offset, length - offset);
    }
}
