using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users.Trading;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

public class RoomUserTradeCancelOfferItemEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    
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

        if (roomUser.Trade == null || roomUser.TradeStatus > 0)
        {
            return;
        }

        var player = client.Player;
        var playerItem = player.Player.FurnitureItems.FirstOrDefault(x => x.Id == ItemId);

        if (playerItem == null)
        {
            return;
        }

        foreach (var user in roomUser.Trade.Users)
        {
            user.TradeStatus = 0;
        }
        
        roomUser.Trade.RemoveOfferedItem(playerItem);
        
        await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeUpdateWriter
        {
            Trade = roomUser.Trade
        });
    }
}