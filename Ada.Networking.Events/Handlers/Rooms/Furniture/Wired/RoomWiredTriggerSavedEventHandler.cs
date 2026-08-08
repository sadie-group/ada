using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.WordFilter;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Constants;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture.Wired;

[PacketId(EventHandlerId.RoomWiredTriggerSaved)]
public class RoomWiredTriggerSavedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomWiredService wiredService,
    ServerRoomConstants roomConstants,
    IWordFilterService wordFilterService,
    IMapper mapper) : INetworkPacketEventHandler
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
            .Room.FurnitureItems
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
                Parameters.FirstOrDefault(),
                roomConstants,
                wordFilterService));

        await client.WriteToStreamAsync(new WiredSavedWriter());
    }
}
