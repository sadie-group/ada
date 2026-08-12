using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Moderation;

[PacketId(EventHandlerId.RoomUserUnban)]
public class RoomUserUnbanEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public int UserId { get; init; }
    public int RoomId { get; init; }

    private Func<Task>? _persist;

    public Task HandleAsync(INetworkClient client)
    {
        var room = roomRepository.TryGetRoomById(RoomId);

        if (room == null || client.Player == null || room.Room.OwnerId != client.Player.Player.Id)
        {
            return Task.CompletedTask;
        }

        var existing = room.Room.PlayerBans.FirstOrDefault(x => x.PlayerId == UserId);

        if (existing != null)
        {
            room.Room.PlayerBans.Remove(existing);
        }

        var roomId = room.Room.Id;
        var playerId = (long) UserId;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.Set<PlayerRoomBan>()
                .Where(x => x.RoomId == roomId && x.PlayerId == playerId)
                .ExecuteDeleteAsync();
        };

        return Task.CompletedTask;
    }

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
