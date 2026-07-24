using System.Buffers;
using System.Runtime.InteropServices;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;

namespace Ada.Networking.Packets;

public class PacketDispatcher(INetworkPacketHandler packetHandler)
{
    public async Task ProcessAsync(INetworkClient client, INetworkPacket packet)
    {
        try
        {
            await packetHandler.HandleAsync(client, packet);
        }
        catch
        {
            // an unhandled exception would fault the ActionBlock and stop all
            // packet processing; handlers own logging their own failures
        }
        finally
        {
            if (packet is NetworkPacket p && MemoryMarshal.TryGetArray(p.Data, out var segment))
            {
                ArrayPool<byte>.Shared.Return(segment.Array!);
            }
        }
    }
}