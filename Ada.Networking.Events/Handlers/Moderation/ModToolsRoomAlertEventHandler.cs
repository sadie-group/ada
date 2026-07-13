using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsRoomAlert)]
public class ModToolsRoomAlertEventHandler : INetworkPacketEventHandler
{
    public int Type { get; set; }
    public required string Message { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        await client.RoomUser?.Room.BroadcastDataAsync(new PlayerAlertWriter
        {
            Message = Message
        })!;
    }
}