using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

[PacketId(EventHandlerId.RoomUserTradeConfirm)]
public class RoomUserTradeConfirmEventHandler(IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
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

        roomUser.TradeStatus = 2;
        
        await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeStatusWriter
        {
            UserId = roomUser.Player.Player.Id,
            Status = roomUser.TradeStatus
        });

        if (roomUser.Trade.Users.All(x => x.TradeStatus == 2))
        {
            var swapped = await roomUser.Trade.SwapItemsAsync();

            await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeCloseWindowWriter());

            if (swapped)
            {
                await roomUser.Trade.BroadcastToUsersAsync(new RoomUserTradeCompletedWriter());
            }

            foreach (var user in roomUser.Trade.Users)
            {
                user.Trade = null;
                user.TradeStatus = 0;
            }
        }
    }
}