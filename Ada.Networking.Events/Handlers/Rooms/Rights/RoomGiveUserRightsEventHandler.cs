using Ada.API.DTOs.Rooms.Rights;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Rooms.Rights;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Rights;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Rights;

[PacketId(EventHandlerId.RoomGiveUserRights)]
public class RoomGiveUserRightsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository,
    IMapper mapper) : INetworkPacketEventHandler, IDefersPersistence
{
    public int PlayerId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var playerId = PlayerId;
        var player = client.Player;
        
        if (player == null)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room == null || room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        if (room.Room.PlayerRights.FirstOrDefault(x => x.PlayerId == playerId) != null)
        {
            return;
        }

        room.UserRepository.TryGetById(playerId, out var targetRoomUser);

        await room.BroadcastDataAsync(new RoomGiveUserRightsWriter
        {
            RoomId = room.Room.Id,
            PlayerId = playerId,
            PlayerUsername = targetRoomUser?.Player.Player.Username
                             ?? await playerRepository.GetPlayerUsernameByIdAsync(playerId)
                             ?? string.Empty
        });

        if (targetRoomUser != null)
        {
            targetRoomUser.ControllerLevel = RoomControllerLevel.Rights;
            targetRoomUser.ApplyFlatCtrlStatus();
            
            await targetRoomUser.NetworkObject.WriteToStreamAsync(new RoomRightsWriter
            {
                ControllerLevel = (int) targetRoomUser.ControllerLevel
            });
        }
        
        var roomPlayerRight = new RoomPlayerRightDto
        {
            RoomId = room.Room.Id,
            PlayerId = playerId,
            CreatedAt = DateTime.Now
        };
        
        room.Room.PlayerRights.Add(roomPlayerRight);
        
        var entity = mapper.Map<RoomPlayerRight>(roomPlayerRight);
        
        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            dbContext.RoomPlayerRights.Add(entity);
            await dbContext.SaveChangesAsync();
        };
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
