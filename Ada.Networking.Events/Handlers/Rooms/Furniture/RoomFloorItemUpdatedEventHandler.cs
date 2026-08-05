using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomFloorFurnitureItemUpdated)]
public class RoomFloorItemUpdatedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomFurnitureItemInteractorRepository interactorRepository,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService)
    : INetworkPacketEventHandler, IDefersPersistence
{
    public int ItemId { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Direction { get; init; }

    private ItemPlacement? _placementToPersist;

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            return;
        }

        var itemId = ItemId;

        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);

        if (room == null)
        {
            return;
        }

        if (!client.RoomUser.HasRights())
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MissingRights);
            return;
        }

        var roomFurnitureItem = room.Room.FurnitureItems.FirstOrDefault(x => x.PlayerFurnitureItemId == itemId);

        if (roomFurnitureItem == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        var furnitureItem = roomFurnitureItem
            .PlayerFurnitureItem
            .FurnitureItem;

        var oldPoints = tileMapHelperService.GetPointsForPlacement(
            roomFurnitureItem.PositionX,
            roomFurnitureItem.PositionY,
            furnitureItem.TileSpanX,
            furnitureItem.TileSpanY,
            roomFurnitureItem.Direction);

        var newPoints = tileMapHelperService.GetPointsForPlacement(
            X, Y,
            furnitureItem.TileSpanX,
            furnitureItem.TileSpanY,
            (HDirection) Direction);

        tileMapHelperService.UpdateTileMapsForPoints(oldPoints,
            room.TileMap,
            room.Room.FurnitureItems,
            excludeItem: roomFurnitureItem);

        var rotatingSingleTileItem = newPoints.Count == 1 &&
             newPoints[0].X == roomFurnitureItem.PositionX &&
             newPoints[0].Y == roomFurnitureItem.PositionY;

        var checkPointsForUsers = furnitureItem is
        {
            CanSit: false,
            CanLay: false
        };

        if (!rotatingSingleTileItem &&
            !tileMapHelperService.CanPlaceAt(newPoints, room.TileMap, checkPointsForUsers))
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        var z = tileMapHelperService.GetItemPlacementHeight(
            room.TileMap,
            newPoints,
            room.Room.FurnitureItems,
            excludeItem: roomFurnitureItem);

        roomFurnitureItem.PositionX = X;
        roomFurnitureItem.PositionY = Y;
        roomFurnitureItem.PositionZ = z;
        roomFurnitureItem.Direction = (HDirection) Direction;

        tileMapHelperService.InvalidateItemIndex(room.Room.FurnitureItems);

        room.TileMap.Map[roomFurnitureItem.PositionY, roomFurnitureItem.PositionX] =
            (short) tileMapHelperService.GetTileState(
                roomFurnitureItem.PositionX,
                roomFurnitureItem.PositionY,
                room.Room.FurnitureItems);

        var interactors = interactorRepository
            .GetInteractorsForType(furnitureItem.InteractionType ?? "");

        foreach (var interactor in interactors)
        {
            await interactor.OnMoveAsync(room, roomFurnitureItem, client.RoomUser);
        }

        foreach (var user in tileMapHelperService.GetUsersAtPoints(oldPoints, room.UserRepository.GetAll()))
        {
            user.CheckStatusForCurrentTile();
        }

        foreach (var user in tileMapHelperService.GetUsersAtPoints(newPoints, room.UserRepository.GetAll()))
        {
            user.CheckStatusForCurrentTile();
        }

        tileMapHelperService.UpdateTileMapsForPoints(oldPoints, room.TileMap, room.Room.FurnitureItems);
        tileMapHelperService.UpdateTileMapsForPoints(newPoints, room.TileMap, room.Room.FurnitureItems);

        await roomFurnitureItemHelperService.BroadcastItemUpdateToRoomAsync(room, roomFurnitureItem);

        _placementToPersist = new ItemPlacement(
            roomFurnitureItem.Id,
            roomFurnitureItem.PositionX,
            roomFurnitureItem.PositionY,
            roomFurnitureItem.PositionZ,
            roomFurnitureItem.Direction);
    }

    private readonly record struct ItemPlacement(int Id, int X, int Y, double Z, HDirection Direction);

    public async Task PersistAsync()
    {
        if (_placementToPersist is not { } placement)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.RoomFurnitureItems
            .Where(x => x.Id == placement.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.PositionX, placement.X)
                .SetProperty(x => x.PositionY, placement.Y)
                .SetProperty(x => x.PositionZ, placement.Z)
                .SetProperty(x => x.Direction, placement.Direction));
    }
}
