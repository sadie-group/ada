using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Packets;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Packets;

public class PacketDispatcher(
    INetworkPacketHandler packetHandler,
    ILogger<PacketDispatcher> logger)
{
    public async Task ProcessAsync(INetworkClient client, INetworkPacket packet)
    {
        try
        {
            await packetHandler.HandleAsync(client, packet);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Handler threw for packet {PacketId}", packet.PacketId);
        }
        finally
        {
            PacketBufferPool.Release(packet);
        }
    }
}
