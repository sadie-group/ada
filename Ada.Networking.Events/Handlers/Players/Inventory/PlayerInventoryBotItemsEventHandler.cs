using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.PlayerInventoryBotItems)]
public class PlayerInventoryBotItemsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new PlayerInventoryBotItemsWriter
        {
            Bots = client.Player.Player.Bots.Where(x => x.RoomId is null).ToList()
        });
    }
}