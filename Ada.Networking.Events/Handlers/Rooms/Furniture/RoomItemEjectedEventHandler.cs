using System.Drawing;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Furniture;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomItemEjected)]
public class RoomItemEjectedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IPlayerRepository playerRepository,
    IRoomFurnitureItemInteractorRepository interactorRepository,
    IRoomTileMapHelperService tileMapHelperService) : INetworkPacketEventHandler
{
    public int Category { get; init; }
    public int ItemId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            return;
        }

        var player = client.Player;
        var itemId = ItemId;
        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);

        var roomFurnitureItem = room?
            .Room.FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItemId == itemId);

        if (room == null || roomFurnitureItem == null)
        {
            return;
        }

        var ownsItem = roomFurnitureItem.PlayerFurnitureItem.PlayerId == player.Player.Id;

        if (!ownsItem && !client.RoomUser.HasRights())
        {
            return;
        }

        var interactors = interactorRepository
            .GetInteractorsForType(roomFurnitureItem
                .PlayerFurnitureItem
                .FurnitureItem.InteractionType ?? "");

        foreach (var interactor in interactors)
        {
            await interactor.OnPickUpAsync(room, roomFurnitureItem, client.RoomUser);
        }

        if (roomFurnitureItem
                .PlayerFurnitureItem
                .FurnitureItem.Type == FurnitureItemType.Floor)
        {
            await room.BroadcastDataAsync(new RoomFloorFurnitureItemRemovedWriter
            {
                Id = roomFurnitureItem.PlayerFurnitureItemId.ToString(),
                Expired = false,
                OwnerId = roomFurnitureItem.PlayerFurnitureItem.PlayerId,
                Delay = 0
            });
        }
        else
        {
            await room.BroadcastDataAsync(new RoomWallFurnitureItemRemovedWriter
            {
                Item = roomFurnitureItem
            });
        }

        room.Room.FurnitureItems.Remove(roomFurnitureItem);

        var point = new Point(
            roomFurnitureItem.PositionX,
            roomFurnitureItem.PositionY);

        foreach (var user in tileMapHelperService.GetUsersAtPoints([point], room.UserRepository.GetAll()))
        {
            user.CheckStatusForCurrentTile();
        }

        var owner = ownsItem
            ? client.Player
            : playerRepository.GetPlayerLogicById(roomFurnitureItem.PlayerFurnitureItem.PlayerId);

        var itemRecord = owner?.Player.FurnitureItems
                             .FirstOrDefault(x => x.Id == roomFurnitureItem.PlayerFurnitureItemId)
                         ?? roomFurnitureItem.PlayerFurnitureItem;

        itemRecord.PlacementData = null;
        roomFurnitureItem.PlayerFurnitureItem.PlacementData = null;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.RoomFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.Id)
            .ExecuteDeleteAsync();

        if (ownsItem)
        {
            await client.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
            {
                Count = 1,
                Category = 1,
                FurnitureItems = [itemRecord]
            });

            await client.WriteToStreamAsync(new PlayerInventoryRefreshWriter());

            return;
        }

        if (owner is not { NetworkObject: not null })
        {
            return;
        }

        await owner.NetworkObject.WriteToStreamAsync(new PlayerInventoryUnseenItemsWriter
        {
            Count = 1,
            Category = 1,
            FurnitureItems = [itemRecord]
        });

        await owner.NetworkObject.WriteToStreamAsync(new PlayerInventoryRefreshWriter());
    }
}
