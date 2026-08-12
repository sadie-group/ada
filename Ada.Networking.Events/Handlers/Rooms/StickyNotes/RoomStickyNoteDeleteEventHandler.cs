using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.Furniture;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.StickyNotes;

[PacketId(EventHandlerId.RoomStickyNoteDelete)]
public class RoomStickyNoteDeleteEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public int ItemId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || client.RoomUser?.HasRights() != true)
        {
            return;
        }

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        var item = room?.Room.FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItem.Id == ItemId);

        if (item == null || !StickyNotes.IsStickyNote(item))
        {
            return;
        }

        room!.Room.FurnitureItems.Remove(item);

        var placementId = item.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.RoomFurnitureItems
                .Where(x => x.Id == placementId)
                .ExecuteDeleteAsync();
        };

        await room.BroadcastDataAsync(new RoomWallFurnitureItemRemovedWriter
        {
            Item = item
        });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
