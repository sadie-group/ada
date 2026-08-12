using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Effects;

[PacketId(EventHandlerId.PlayerAvatarEffectActivated)]
public class PlayerAvatarEffectActivatedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, ICountsAsRoomActivity
{
    public int EffectId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out _, out var roomUser))
        {
            return;
        }

        if (EffectId == 0)
        {
            await roomUser.SetEffectAsync(RoomUserEffect.None);
            return;
        }

        var playerId = roomUser.Player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var owned = await dbContext.PlayerEffectItems
            .AnyAsync(x => x.PlayerId == playerId && x.EffectId == EffectId && x.IsActivated);

        if (!owned)
        {
            return;
        }

        await roomUser.SetEffectAsync((RoomUserEffect) EffectId);
    }
}
