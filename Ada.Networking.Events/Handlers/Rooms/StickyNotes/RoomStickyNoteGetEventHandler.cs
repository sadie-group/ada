using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Rooms.StickyNotes;

namespace Ada.Networking.Events.Handlers.Rooms.StickyNotes;

[PacketId(EventHandlerId.RoomStickyNoteGet)]
public class RoomStickyNoteGetEventHandler(IRoomRepository roomRepository) : INetworkPacketEventHandler
{
    public int ItemId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
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

        await client.WriteToStreamAsync(new RoomStickyNoteWriter
        {
            ItemId = ItemId.ToString(),
            Contents = item.PlayerFurnitureItem.MetaData
        });
    }
}
