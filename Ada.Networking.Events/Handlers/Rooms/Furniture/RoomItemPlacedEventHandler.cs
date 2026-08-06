using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomItemPlaced)]
public class RoomItemPlacedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomFurnitureItemInteractorRepository interactorRepository,
    IRoomTileMapHelperService tileMapHelperService,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService,
    IMapper mapper,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public required string PlacementData { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        if (!client.RoomUser.HasRights())
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MissingRights);
            return;
        }

        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);

        if (room == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        var player = client.Player;
        var placementData = PlacementData.Split(" ");

        if (placementData.Length < 4 ||
            !int.TryParse(placementData[0], out var itemId) ||
            itemId == 0)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        var playerItem = player.Player.FurnitureItems.FirstOrDefault(x => x.Id == itemId);

        if (playerItem == null || playerItem.PlacementData != null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        if (playerItem.FurnitureItem.Type == FurnitureItemType.Floor)
        {
            if (!int.TryParse(placementData[1], out var x) ||
                !int.TryParse(placementData[2], out var y) ||
                !int.TryParse(placementData[3], out var direction))
            {
                await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
                return;
            }

            var pointsForPlacement = tileMapHelperService.GetPointsForPlacement(
                x,
                y,
                playerItem.FurnitureItem.TileSpanX,
                playerItem.FurnitureItem.TileSpanY,
                (HDirection) direction);

            if (!pointsForPlacement.All(p => tileMapHelperService.CanPlaceAt([p], room.TileMap)))
            {
                await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
                return;
            }

            var z = tileMapHelperService.GetItemPlacementHeight(
                room.TileMap,
                pointsForPlacement,
                room.Room.FurnitureItems);

            var roomFurniturePlacementData = new PlayerFurnitureItemPlacementDataDto
            {
                RoomId = room.Room.Id,
                PlayerFurnitureItemId = playerItem.Id,
                PlayerFurnitureItem = playerItem,
                PositionX = x,
                PositionY = y,
                PositionZ = z,
                WallPosition = string.Empty,
                Direction = (HDirection) direction,
                CreatedAt = DateTime.Now
            };

            playerItem.PlacementData = roomFurniturePlacementData;
            room.Room.FurnitureItems.Add(roomFurniturePlacementData);

            tileMapHelperService.UpdateTileMapsForPoints(pointsForPlacement, room.TileMap, room.Room.FurnitureItems);

            foreach (var user in tileMapHelperService.GetUsersAtPoints(pointsForPlacement, room.UserRepository.GetAll()))
            {
                user.CheckStatusForCurrentTile();
            }

            await client.WriteToStreamAsync(new PlayerInventoryRemoveItemWriter
            {
                ItemId = playerItem.Id
            });

            var interactors = interactorRepository
                .GetInteractorsForType(roomFurniturePlacementData
                    .PlayerFurnitureItem
                    .FurnitureItem.InteractionType ?? "");

            foreach (var interactor in interactors)
            {
                await interactor.OnPlaceAsync(client.RoomUser.Room, roomFurniturePlacementData, client.RoomUser);
            }

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var roomFurnitureItemEntity = await dbContext.PlayerFurnitureItems
                .FirstAsync(x => x.Id == roomFurniturePlacementData.PlayerFurnitureItem.Id);

            dbContext.Entry(roomFurnitureItemEntity).State = EntityState.Unchanged;

            roomFurnitureItemEntity.PlacementData =
                mapper.Map<PlayerFurnitureItemPlacementData>(roomFurniturePlacementData);

            await dbContext.SaveChangesAsync();

            var roomFurnitureItem = roomFurniturePlacementData.PlayerFurnitureItem.FurnitureItem;

            await room.BroadcastDataAsync(new RoomFloorItemPlacedWriter
            {
                Id = roomFurniturePlacementData.PlayerFurnitureItemId,
                AssetId = roomFurnitureItem.AssetId,
                PositionX = roomFurniturePlacementData.PositionX,
                PositionY = roomFurniturePlacementData.PositionY,
                Direction = (int)roomFurniturePlacementData.Direction,
                PositionZ = roomFurniturePlacementData.PositionZ,
                StackHeight = 0.ToString(),
                Extra = 1,
                ObjectDataKey = (int) roomFurnitureItemHelperService.GetObjectDataKeyForItem(roomFurniturePlacementData),
                ObjectData = roomFurnitureItemHelperService.GetObjectDataForItem(roomFurniturePlacementData),
                MetaData = roomFurniturePlacementData.PlayerFurnitureItem.MetaData,
                Expires = -1,
                InteractionModes = roomFurnitureItem.InteractionModes,
                OwnerId = roomFurniturePlacementData.PlayerFurnitureItem.PlayerId,
                OwnerUsername = player.Player.Username
            });
        }
        else if (playerItem.FurnitureItem.Type == FurnitureItemType.Wall)
        {
            if (playerItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer &&
                room.Room.FurnitureItems.Any(x => x.PlayerFurnitureItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer))
            {
                await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MaxDimmers);
                return;
            }

            var wallPosition = $"{placementData[1]} {placementData[2]} {placementData[3]}";

            var roomFurnitureItem = new PlayerFurnitureItemPlacementDataDto
            {
                RoomId = room.Room.Id,
                PlayerFurnitureItemId = playerItem.Id,
                PlayerFurnitureItem = playerItem,
                PositionX = 0,
                PositionY = 0,
                PositionZ = 0,
                WallPosition = wallPosition,
                Direction = 0,
                CreatedAt = DateTime.Now
            };

            playerItem.PlacementData = roomFurnitureItem;
            room.Room.FurnitureItems.Add(roomFurnitureItem);

            await client.WriteToStreamAsync(new PlayerInventoryRemoveItemWriter
            {
                ItemId = itemId
            });

            var interactors = interactorRepository
                .GetInteractorsForType(roomFurnitureItem
                    .PlayerFurnitureItem
                    .FurnitureItem.InteractionType ?? "");

            foreach (var interactor in interactors)
            {
                await interactor.OnPlaceAsync(client.RoomUser.Room, roomFurnitureItem, client.RoomUser);
            }

            var ownerUsername = await playerRepository.GetPlayerUsernameByIdAsync(
                roomFurnitureItem.PlayerFurnitureItem.PlayerId);

            await room.BroadcastDataAsync(new RoomWallFurnitureItemPlacedWriter
            {
                RoomFurnitureItem = roomFurnitureItem,
                OwnerUsername = ownerUsername ?? "Unknown User"
            });

            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            var playerItemEntity = await dbContext.PlayerFurnitureItems
                .FirstAsync(x => x.Id == playerItem.Id);

            playerItemEntity.PlacementData = mapper.Map<PlayerFurnitureItemPlacementData>(roomFurnitureItem);

            await dbContext.SaveChangesAsync();
        }
    }
}
