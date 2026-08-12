using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users.Trading;
using Ada.Core.Shared.Attributes;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Ada.Networking.Writers;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

[PacketId(ServerPacketId.RoomUserTradeCancel)]
public class RoomUserCancelTradeEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return;
        }

        if (roomUser.Trade == null)
        {
            return;
        }
        
        foreach (var user in roomUser.Trade.Users)
        {
            user.Trade = null;
            user.TradeStatus = 0;
        }
        
        await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeUpdateWriter
        {
            Trade = roomUser.Trade
        });
        
        await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeClosedWriter
        {
            UserId = roomUser.Player.Player.Id,
            Reason = RoomUserTradeCloseReason.Cancelled
        });
    }
}