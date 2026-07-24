using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Extensions;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Rooms.Users;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerChangedMotto)]
public class PlayerChangedMottoEventHandler(
    IRoomRepository roomRepository, 
    ServerPlayerConstants constants,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public required string Motto { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player?.Player.AvatarData == null)
        {
            return;
        }
        
        var player = client.Player!;
        var newMotto = Motto.Truncate(constants.MaxMottoLength);
        
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }
        
        await room.BroadcastDataAsync(new RoomUserDataWriter{
            Users = [roomUser]
        });

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        await dbContext.PlayerAvatarData
            .Where(x => x.PlayerId == player.Player.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.Motto, newMotto));

        player.Player.AvatarData.Motto = newMotto;
    }
}