using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;
using Ada.Db;
using Ada.Networking.Events.Attributes;
using Ada.Networking.Writers.Rooms.Furniture;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture.Wired;

[PacketId(EventHandlerId.RoomWiredTriggerSaved)]
public class RoomWiredTriggerSavedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomWiredService wiredService,
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

        var roomItems = room!
            .Room
            .FurnitureItems
            .Where(x => ItemIds.Contains(x.Id))
            .ToList();

        await wiredService.SaveSettingsAsync(
            roomItem,
            new PlayerFurnitureItemWiredDataDto
            {
                PlayerFurnitureItemPlacementDataId = roomItem.Id,
                PlacementData = roomItem,
                SelectedItems = roomItems,
                Message = Input,
                IntParameters = WiredParameterHelpers.Serialize(Parameters),
                Delay = Parameters.FirstOrDefault()
            });
        
        await client.WriteToStreamAsync(new WiredSavedWriter());
    }
}