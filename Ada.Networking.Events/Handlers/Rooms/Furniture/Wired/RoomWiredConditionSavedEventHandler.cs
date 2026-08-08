using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db.Models.Constants;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture.Wired;

[PacketId(EventHandlerId.RoomWiredConditionSaved)]
public class RoomWiredConditionSavedEventHandler(
    IRoomWiredService wiredService,
    ServerRoomConstants roomConstants,
    IWordFilterService wordFilterService) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }
    public required List<int> Parameters { get; init; }
    public required string Input { get; init; }
    public required List<int> ItemIds { get; init; }
    public required int SelectionCode { get; init; }

    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        var room = client.RoomUser?.Room;

        var roomItem = room?
            .Room
            .FurnitureItems
            .FirstOrDefault(x => x.Id == ItemId);

        if (roomItem == null)
        {
            return;
        }

        await wiredService.SaveSettingsAsync(
            roomItem,
            WiredSettingsHelpers.Build(
                roomItem,
                room!.Room.FurnitureItems.Where(x => ItemIds.Contains(x.Id)),
                Input,
                Parameters,
                delayInPulses: 0,
                roomConstants,
                wordFilterService));

        await client.WriteToStreamAsync(new WiredSavedWriter());
    }
}
