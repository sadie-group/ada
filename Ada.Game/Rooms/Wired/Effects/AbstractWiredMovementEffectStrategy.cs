using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Miscellaneous;

namespace Ada.Game.Rooms.Wired.Effects;

public abstract class AbstractWiredMovementEffectStrategy(
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService furnitureItemHelperService) : IWiredEffectStrategy
{
    public abstract string InteractionType { get; }

    public async Task ExecuteAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        IRoomUser? userWhoTriggered)
    {
        var selectedItems = effect.WiredData?.SelectedItems;

        if (selectedItems == null || selectedItems.Count == 0)
        {
            return;
        }

        foreach (var selected in selectedItems)
        {
            var roomItem = room.Room.FurnitureItems.FirstOrDefault(x => x.Id == selected.Id);

            if (roomItem == null)
            {
                continue;
            }

            await MoveItemAsync(room, effect, roomItem);
        }
    }

    protected abstract Task MoveItemAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto effect,
        PlayerFurnitureItemPlacementDataDto item);

    protected async Task<bool> TryMoveInDirectionAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto item,
        HDirection direction)
    {
        var nextPoint = tileMapHelperService.GetPointInFront(item.PositionX, item.PositionY, direction);

        if (!tileMapHelperService.CanPlaceAt(
                [nextPoint],
                room.TileMap,
                room.Room.FurnitureItems.Except([item]).ToList()))
        {
            return false;
        }

        item.PositionX = nextPoint.X;
        item.PositionY = nextPoint.Y;

        tileMapHelperService.InvalidateItemIndex(room.Room.FurnitureItems);

        await furnitureItemHelperService.BroadcastItemUpdateToRoomAsync(room, item);
        return true;
    }

    protected static HDirection GetDirectionTowards(Point from, Point to)
    {
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;

        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return deltaX >= 0 ? HDirection.East : HDirection.West;
        }

        return deltaY >= 0 ? HDirection.South : HDirection.North;
    }

    protected static HDirection GetOppositeDirection(HDirection direction)
    {
        return (HDirection)(((int)direction + 4) % 8);
    }

    protected IRoomUser? GetClosestUser(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item)
    {
        return room.UserRepository
            .GetAll()
            .OrderBy(x =>
                Math.Abs(x.Point.X - item.PositionX) +
                Math.Abs(x.Point.Y - item.PositionY))
            .FirstOrDefault();
    }
}
