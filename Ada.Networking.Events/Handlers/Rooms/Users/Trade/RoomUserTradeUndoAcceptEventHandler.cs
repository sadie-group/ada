using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users.Trading;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

public class RoomUserTradeUndoAcceptEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return;
        }

        if (roomUser.Trade == null || roomUser.TradeStatus < 1)
        {
            return;
        }

        roomUser.TradeStatus = 0;
        
        await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeStatusWriter
        {
            UserId = roomUser.Player.Player.Id,
            Status = roomUser.TradeStatus
        });
    }
}