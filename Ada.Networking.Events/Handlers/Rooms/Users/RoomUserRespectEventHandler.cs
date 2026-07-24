using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Users;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Rooms.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Users;

[PacketId(EventHandlerId.RoomUserRespect)]
public class RoomUserRespectEventHandler(
    IPlayerRepository playerRepository,
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper)
    : INetworkPacketEventHandler
{
    public int TargetId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository, client, out var room, out var roomUser))
        {
            return;
        }
        
        var player = client.Player!;
        var playerData = player.Player.Data;
        var lastRoom = player.State.CurrentRoomId;
        var targetPlayer = playerRepository.GetPlayerLogicById(TargetId);
        
        if (playerData.RespectPoints < 1 || 
            player.Player.Id == TargetId || 
            targetPlayer == null || 
            targetPlayer.State.CurrentRoomId != 0 && lastRoom != targetPlayer.State.CurrentRoomId)
        {
            return;
        }

        var respect = new PlayerRespectDto
        {
            OriginPlayerId = player.Player.Id,
            TargetPlayerId = targetPlayer.Player.Id
        };

        playerData.RespectPoints--;
        targetPlayer.Player.Respects.Add(respect);

        var respectEntity = mapper.Map<PlayerRespect>(respect);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.PlayerRespects.Add(respectEntity);
        dbContext.Entry(playerData).Property(x => x.RespectPoints).IsModified = true;
        await dbContext.SaveChangesAsync();

        await room.BroadcastDataAsync(new RoomUserRespectWriter
        {
            UserId = TargetId,
            TotalRespects = targetPlayer.Player.Respects.Count
        });
        
        await room.BroadcastDataAsync(new RoomUserActionWriter
        {
            UserId = roomUser.Player.Player.Id,
            Action = (int) RoomUserAction.ThumbsUp
        });
    }
}