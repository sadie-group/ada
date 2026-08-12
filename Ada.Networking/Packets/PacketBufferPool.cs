using System.Buffers;
using System.Runtime.InteropServices;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public static class PacketBufferPool
{
    public static void Return(byte[] buffer)
    {
        if (buffer.Length != 0)
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static void Release(INetworkPacket packet)
    {
        if (packet is not NetworkPacket networkPacket)
        {
            return;
        }

        if (MemoryMarshal.TryGetArray(networkPacket.Data, out var segment) && segment.Array is { Length: > 0 })
        {
            ArrayPool<byte>.Shared.Return(segment.Array);
        }
    }
}
