using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

[PacketId(EventHandlerId.RoomUserTradeOfferItems)]
public class RoomUserTradeOfferItemsEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public List<int> Ids { get; init; } = [];

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return;
        }

        if (roomUser.Trade == null)
        {
            return;
        }

        var player = client.Player;
        var items = new List<PlayerFurnitureItemDto>();

        foreach (var id in Ids)
        {
            var playerItem = player.Player.FurnitureItems.FirstOrDefault(x => x.Id == id);

            if (playerItem is not { PlacementData: null })
            {
                return;
            }

            items.Add(playerItem);
        }

        await roomUser.Trade.OfferItemsAsync(items);
    }
}
