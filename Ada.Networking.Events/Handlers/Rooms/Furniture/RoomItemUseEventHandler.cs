using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Networking.Client;
using Ada.Core.Enums.Game.Furniture;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Events.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomItemUse)]
public class RoomItemUseEventHandler(
    IRoomFurnitureItemInteractorRepository interactorRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IRoomWiredService wiredService) : INetworkPacketEventHandler
{
    public int ItemId { get; init; }
    
    [RequiresRoomRights]
    public async Task HandleAsync(INetworkClient client)
    {
        var room = client.RoomUser!.Room;

        var roomFurnitureItem = room
                .Room.FurnitureItems
                .FirstOrDefault(x => x.PlayerFurnitureItemId == ItemId);

        if (roomFurnitureItem == null)
        {
            return;
        }
        
        var interactors = interactorRepository
            .GetInteractorsForType(roomFurnitureItem
                .PlayerFurnitureItem
                .FurnitureItem.InteractionType ?? "");

        if (!interactors.Any())
        {
            await roomFurnitureItemHelperService.CycleInteractionStateForItemAsync(
                room, roomFurnitureItem);
        }
        else
        {
            foreach (var interactor in interactors)
            {
                await interactor.OnTriggerAsync(room, roomFurnitureItem, client.RoomUser);
            }
        }

        var stateTriggers = wiredService.GetTriggers(
            FurnitureItemInteractionType.WiredTriggerFurnitureStateChanged,
            room.Room.FurnitureItems,
            requiredSelectedIds: [roomFurnitureItem.Id]);

        foreach (var trigger in stateTriggers)
        {
            await wiredService.RunTriggerForRoomAsync(room, trigger, client.RoomUser);
        }
    }
}