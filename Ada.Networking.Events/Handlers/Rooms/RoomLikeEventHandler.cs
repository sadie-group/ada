using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms;

[PacketId(EventHandlerId.RoomLike)]
public class RoomLikeEventHandler(IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper) : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out _))
        {
            return;
        }

        if (room.Room.OwnerId == client.Player.Player.Id || client.Player.Player.RoomLikes.FirstOrDefault(x => x.RoomId == room.Room.Id) != null)
        {
            return;
        }

        var roomLike = new PlayerRoomLikeDto()
        {
            PlayerId = client.Player.Player.Id,
            RoomId = room.Room.Id
        };
        
        client.Player.Player.RoomLikes.Add(roomLike);
        
        var roomLikeEntity = mapper.Map<PlayerRoomLike>(roomLike);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerRoomLikes.Add(roomLikeEntity);
        await dbContext.SaveChangesAsync();
    }
}