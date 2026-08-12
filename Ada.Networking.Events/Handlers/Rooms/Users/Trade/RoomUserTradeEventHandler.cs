using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users.Trading;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms.Users;
using Ada.Game.Rooms;
using Ada.Networking.Writers.Rooms.Users.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Rooms.Users.Trade;

[PacketId(EventHandlerId.RoomUserTrade)]
public class RoomUserTradeEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IPlayerHelperService playerHelperService,
    ILogger<RoomUserTrade> tradeLogger) : INetworkPacketEventHandler
{
    public required int TargetUserId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }

        if (roomUser.Player.Player.Id == TargetUserId ||
            !room.UserRepository.TryGetById(TargetUserId, out var targetUser) ||
            targetUser == null)
        {
            return;
        }

        var roomSettings = room.Room.Settings;

        if (roomSettings == null)
        {
            return;
        }

        var tradingPermitted = roomSettings.TradeOption switch
        {
            RoomTradeOption.Allowed => true,
            RoomTradeOption.RequiresRights => roomUser.HasRights(),
            _ => false
        };

        if (!tradingPermitted)
        {
            await client.WriteToStreamAsync(new RoomUserTradeErrorWriter { Code = RoomUserTradeError.RoomTradingNotAllowed });
            return;
        }

        if (roomUser.Trade != null)
        {
            await client.WriteToStreamAsync(new RoomUserTradeErrorWriter { Code = RoomUserTradeError.SelfAlreadyTrading });
            return;
        }

        if (targetUser.Trade != null)
        {
            await client.WriteToStreamAsync(new RoomUserTradeErrorWriter { Code = RoomUserTradeError.TargetAlreadyTrading });
            return;
        }

        await roomUser.NetworkObject.WriteToStreamAsync(writer: new RoomUserTradeStartedWriter
        {
            UserIds = [roomUser.Player.Player.Id, targetUser.Player.Player.Id],
            State = 1
        });

        await targetUser.NetworkObject.WriteToStreamAsync(new RoomUserTradeStartedWriter
        {
            UserIds = [roomUser.Player.Player.Id, targetUser.Player.Player.Id],
            State = 1
        });

        var trade = new RoomUserTrade(playerHelperService, dbContextFactory, tradeLogger)
        {
            Users = [roomUser, targetUser],
            Items = []
        };

        roomUser.Trade = trade;
        targetUser.Trade = trade;
    }
}
