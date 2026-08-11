using Ada.API.DTOs.Rooms.Rights;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Rights;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Rights;

[PacketId(EventHandlerId.RoomRemoveUserRights)]
public class RoomRemoveUserRightsEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required List<int> Ids { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
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

        foreach (var playerId in Ids)
        {
            var right = room.Room.PlayerRights.FirstOrDefault(x => x.PlayerId == playerId);

            if (right == null)
            {
                continue;
            }

            await RemoveRoomPlayerRightAsync(playerId, room, right);
        }

        if (_removedPlayerIds.Count == 0)
        {
            return;
        }

        var roomId = room.Room.Id;
        var removed = _removedPlayerIds;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.RoomPlayerRights
                .Where(x => x.RoomId == roomId && removed.Contains(x.PlayerId))
                .ExecuteDeleteAsync();
        };
    }

    private async Task RemoveRoomPlayerRightAsync(long playerId, IRoomLogic room, RoomPlayerRightDto right)
    {
        if (room.UserRepository.TryGetById((int) playerId, out var roomUser))
        {
            roomUser!.ControllerLevel = RoomControllerLevel.None;
            roomUser.ApplyFlatCtrlStatus();

            await roomUser.NetworkObject.WriteToStreamAsync(new RoomRightsWriter
            {
                ControllerLevel = (int) roomUser.ControllerLevel
            });
        }

        room.Room.PlayerRights.Remove(right);

        _removedPlayerIds.Add(playerId);

        await room.BroadcastDataAsync(
            new RoomRemoveUserRightsWriter
            {
                RoomId = room.Room.Id,
                PlayerId = playerId
            });
    }

    private readonly List<long> _removedPlayerIds = [];

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
