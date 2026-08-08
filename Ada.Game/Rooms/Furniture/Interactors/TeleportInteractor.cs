using System.Drawing;
using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Extensions;
using Ada.Db;
using Ada.Networking.Events;
using Ada.Networking.Writers.Rooms.Users;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class TeleportInteractor(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IRoomDeferralScheduler deferralScheduler,
    ILogger<TeleportInteractor> logger) : AbstractRoomFurnitureItemInteractor
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.Teleport];

    private readonly TimeSpan _delay = TimeSpan.FromMilliseconds(500);
    
    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var itemPosition = new Point(item.PositionX, item.PositionY);
        var itemInFront = tileMapHelperService.GetPointInFront(item.PositionX, item.PositionY, item.Direction);
        
        if (roomUser.Point == itemPosition)
        {
            roomUser.CanWalk = false;
            
            var facingDirection = tileMapHelperService.GetOppositeDirection(item.Direction);
        
            roomUser.Direction = facingDirection;
            roomUser.DirectionHead = facingDirection;
            roomUser.NeedsUpdate = true;

            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1");
            await UseTeleportAsync(room, item, roomUser);
        }
        else if (roomUser.Point == itemInFront)
        {
            roomUser.CanWalk = false;
            
            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "1");

            roomUser.OverridePoints.Add(itemPosition);

            roomUser.WalkToPoint(itemPosition, async () =>
            {
                roomUser.OverridePoints.Remove(itemPosition);

                if (item.PlayerFurnitureItem.FurnitureItem.InteractionModes == 1)
                {
                    await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "2");

                    deferralScheduler.Schedule(room, _delay, async () =>
                    {
                        await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");
                        await UseTeleportAsync(room, item, roomUser);
                    }, "teleport entry settle");

                    return;
                }

                await UseTeleportAsync(room, item, roomUser);
            });
        }
        else
        {
            roomUser.WalkToPoint(itemInFront, OnReachedGoal);
            return;

            void OnReachedGoal()
                => OnTriggerAsync(room, item, roomUser)
                    .FireAndForget(logger, "teleport walk-to-goal trigger");
        }
    }

    private async Task UseTeleportAsync(
        IRoomLogic room,
        PlayerFurnitureItemPlacementDataDto item,
        IRoomUser roomUser)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var link = await dbContext
            .PlayerFurnitureItemLinks
            .Where(x => x.ParentId == item.PlayerFurnitureItemId || x.ChildId == item.PlayerFurnitureItemId)
            .FirstOrDefaultAsync();

        if (link == null)
        {
            return;
        }

        var targetItemId = link.ParentId == item.PlayerFurnitureItemId ? link.ChildId : link.ParentId;
        
        var targetRoomItem = room
            .Room
            .FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItemId == targetItemId);
        
        if (targetRoomItem != null)
        {
            await UseTeleportInSameRoomAsync(
                roomUser,
                item,
                targetRoomItem,
                room);

            return;
        }

        await UseTeleportInDifferentRoomAsync(
            roomUser,
            item,
            targetItemId,
            room);
    }

    private async Task UseTeleportInDifferentRoomAsync(
        IRoomUser roomUser, 
        PlayerFurnitureItemPlacementDataDto item,
        long targetItemId,
        IRoomLogic room)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var targetRoomId = await dbContext
            .RoomFurnitureItems
            .Where(x => x.PlayerFurnitureItemId == targetItemId)
            .Select(x => x.RoomId)
            .FirstOrDefaultAsync();

        if (targetRoomId != 0)
        {
            var targetRoom = await RoomHelpers.TryLoadRoomByIdAsync(targetRoomId,
                roomRepository,
                dbContextFactory,
                mapper);

            var targetItem = targetRoom?.Room.FurnitureItems
                .FirstOrDefault(x => x.PlayerFurnitureItemId == targetItemId);

            if (targetItem != null)
            {
                roomUser.Player.State.Teleport = targetItem;

                await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(targetRoom!, targetItem, "2");

                deferralScheduler.Schedule(room, _delay, async () =>
                {
                    await roomUser.NetworkObject.WriteToStreamAsync(new RoomForwardEntryWriter
                    {
                        RoomId = targetRoomId
                    });

                    await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(targetRoom!, targetItem, "1");
                    await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");
                }, "teleport cross-room settle");
            }
        }
    }

    private async Task UseTeleportInSameRoomAsync(
        IRoomUser roomUser,
        PlayerFurnitureItemPlacementDataDto item,
        PlayerFurnitureItemPlacementDataDto targetItem,
        IRoomLogic room)
    {
        if (item.PlayerFurnitureItem.FurnitureItem.InteractionModes == 1)
        {
            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, targetItem, "2");

            deferralScheduler.Schedule(room, _delay, ArriveAsync, "teleport same-room settle");

            return;
        }

        await ArriveAsync();
        return;

        async Task ArriveAsync()
        {
            var newPoint = new Point(targetItem.PositionX, targetItem.PositionY);

            room.TileMap.UnitMap[roomUser.Point].Remove(roomUser);
            room.TileMap.AddUnitToMap(newPoint, roomUser);

            await roomUser.SetPositionAsync(newPoint);

            roomUser.Direction = targetItem.Direction;
            roomUser.DirectionHead = targetItem.Direction;
            roomUser.NeedsUpdate = true;

            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, targetItem, "1");

            var squareInFront = tileMapHelperService.GetPointInFront(
                targetItem.PositionX,
                targetItem.PositionY,
                targetItem.Direction);

            roomUser.WalkToPoint(squareInFront, OnReachedGoal);
        }

        void OnReachedGoal()
            => CompleteTeleportAsync().FireAndForget(logger, "teleport exit walk-to-goal");

        async Task CompleteTeleportAsync()
        {
            await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, item, "0");

            deferralScheduler.Schedule(room, _delay, async () =>
            {
                await roomFurnitureItemHelperService.UpdateMetaDataForItemAsync(room, targetItem, "0");

                roomUser.CanWalk = true;
            }, "teleport exit settle");
        }
    }
}