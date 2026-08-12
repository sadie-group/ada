using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Rooms.StickyNotes;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.StickyNotes;

[PacketId(EventHandlerId.RoomStickyNoteUpdate)]
public class RoomStickyNoteUpdateEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public int ItemId { get; init; }
    public required string Contents { get; init; }

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

        var contents = StickyNotes.Sanitise(Contents);

        item.PlayerFurnitureItem.MetaData = contents;

        var furnitureItemId = item.PlayerFurnitureItem.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerFurnitureItems
                .Where(x => x.Id == furnitureItemId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.MetaData, contents));
        };

        await room!.BroadcastDataAsync(new RoomStickyNoteWriter
        {
            ItemId = ItemId.ToString(),
            Contents = contents
        });
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
