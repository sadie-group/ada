using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomHeightmap2)]
public class RoomHeightmap2EventHandler(RoomHeightmapEventHandler eventHandler) : INetworkPacketEventHandler
{
    // Nitro sends a different header based on if the user is exiting a room to enter another
    // Just call the original / other event
    
    public async Task HandleAsync(INetworkClient client)
    {
        await eventHandler.HandleAsync(client);
    }
}