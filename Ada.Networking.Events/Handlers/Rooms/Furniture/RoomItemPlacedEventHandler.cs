using System.Text.RegularExpressions;
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
    IPlayerRepository playerRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required string PlacementData { get; init; }

    private const int _maxWallPositionLength = 64;

    private static readonly Regex _wallPositionPattern =
        new(@"\A:w=-?\d{1,4},-?\d{1,4} l=-?\d{1,4},-?\d{1,4} [lr]\z", RegexOptions.Compiled);

    private static bool IsValidWallPosition(string wallPosition)
        => wallPosition.Length <= _maxWallPositionLength &&
           _wallPositionPattern.IsMatch(wallPosition);

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;

    public async Task HandleAsync(INetworkClient client)
    {
        var context = await ResolveContextAsync(client);

        if (context == null)
        {
            return;
        }

        var (room, player, playerItem, placementData) = context.Value;

        switch (playerItem.FurnitureItem.Type)
        {
            case FurnitureItemType.Floor:
                await PlaceFloorItemAsync(client, room, player, playerItem, placementData);
                break;
            case FurnitureItemType.Wall:
                await PlaceWallItemAsync(client, room, playerItem, placementData);
                break;
        }
    }

    private readonly record struct PlacementContext(
        IRoomLogic Room,
        IPlayerLogic Player,
        PlayerFurnitureItemDto PlayerItem,
        string[] PlacementData);

    private async Task<PlacementContext?> ResolveContextAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return null;
        }

        if (!client.RoomUser.HasRights())
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MissingRights);
            return null;
        }

        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);

        if (room == null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return null;
        }

        var placementData = PlacementData.Split(" ");

        if (placementData.Length < 4 ||
            !int.TryParse(placementData[0], out var itemId) ||
            itemId == 0)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return null;
        }

        var playerItem = client.Player.Player.FurnitureItems.FirstOrDefault(x => x.Id == itemId);

        if (playerItem == null || playerItem.PlacementData != null)
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return null;
        }

        return new PlacementContext(room, client.Player, playerItem, placementData);
    }

    private async Task PlaceFloorItemAsync(
        INetworkClient client,
        IRoomLogic room,
        IPlayerLogic player,
        PlayerFurnitureItemDto playerItem,
        string[] placementData)
    {
        if (!int.TryParse(placementData[1], out var x) ||
            !int.TryParse(placementData[2], out var y) ||
            !int.TryParse(placementData[3], out var direction) ||
            !Enum.IsDefined(typeof(HDirection), direction))
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

        var placement = new PlayerFurnitureItemPlacementDataDto
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

        playerItem.PlacementData = placement;
        room.Room.FurnitureItems.Add(placement);

        tileMapHelperService.UpdateTileMapsForPoints(pointsForPlacement, room.TileMap, room.Room.FurnitureItems);

        foreach (var user in tileMapHelperService.GetUsersAtPoints(pointsForPlacement, room.UserRepository.GetAll()))
        {
            user.CheckStatusForCurrentTile();
        }

        await client.WriteToStreamAsync(new PlayerInventoryRemoveItemWriter
        {
            ItemId = playerItem.Id
        });

        await RunPlacementInteractorsAsync(client, placement);

        _persist = () => PersistPlacementAsync(placement, detachBeforeAssigning: true);

        var furnitureItem = placement.PlayerFurnitureItem.FurnitureItem;

        await room.BroadcastDataAsync(new RoomFloorItemPlacedWriter
        {
            Id = placement.PlayerFurnitureItemId,
            AssetId = furnitureItem.AssetId,
            PositionX = placement.PositionX,
            PositionY = placement.PositionY,
            Direction = (int) placement.Direction,
            PositionZ = placement.PositionZ,
            StackHeight = 0.ToString(),
            Extra = 1,
            ObjectDataKey = (int) roomFurnitureItemHelperService.GetObjectDataKeyForItem(placement),
            ObjectData = roomFurnitureItemHelperService.GetObjectDataForItem(placement),
            MetaData = placement.PlayerFurnitureItem.MetaData,
            Expires = -1,
            InteractionModes = furnitureItem.InteractionModes,
            OwnerId = placement.PlayerFurnitureItem.PlayerId,
            OwnerUsername = player.Player.Username
        });
    }

    private async Task PlaceWallItemAsync(
        INetworkClient client,
        IRoomLogic room,
        PlayerFurnitureItemDto playerItem,
        string[] placementData)
    {
        if (playerItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer &&
            room.Room.FurnitureItems.Any(x =>
                x.PlayerFurnitureItem.FurnitureItem.InteractionType == FurnitureItemInteractionType.Dimmer))
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MaxDimmers);
            return;
        }

        var wallPosition = $"{placementData[1]} {placementData[2]} {placementData[3]}";

        if (!IsValidWallPosition(wallPosition))
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.CantSetItem);
            return;
        }

        var placement = new PlayerFurnitureItemPlacementDataDto
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

        playerItem.PlacementData = placement;
        room.Room.FurnitureItems.Add(placement);

        await client.WriteToStreamAsync(new PlayerInventoryRemoveItemWriter
        {
            ItemId = playerItem.Id
        });

        await RunPlacementInteractorsAsync(client, placement);

        var ownerUsername = await playerRepository.GetPlayerUsernameByIdAsync(
            placement.PlayerFurnitureItem.PlayerId);

        await room.BroadcastDataAsync(new RoomWallFurnitureItemPlacedWriter
        {
            RoomFurnitureItem = placement,
            OwnerUsername = ownerUsername ?? "Unknown User"
        });

        _persist = () => PersistPlacementAsync(placement, detachBeforeAssigning: false);
    }

    private async Task RunPlacementInteractorsAsync(
        INetworkClient client,
        PlayerFurnitureItemPlacementDataDto placement)
    {
        var interactors = interactorRepository
            .GetInteractorsForType(placement.PlayerFurnitureItem.FurnitureItem.InteractionType ?? "");

        foreach (var interactor in interactors)
        {
            await interactor.OnPlaceAsync(client.RoomUser!.Room, placement, client.RoomUser);
        }
    }

    private async Task PersistPlacementAsync(
        PlayerFurnitureItemPlacementDataDto placement,
        bool detachBeforeAssigning)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var entity = await dbContext.PlayerFurnitureItems
            .FirstAsync(x => x.Id == placement.PlayerFurnitureItem.Id);

        if (detachBeforeAssigning)
        {
            dbContext.Entry(entity).State = EntityState.Unchanged;
        }

        entity.PlacementData = mapper.Map<PlayerFurnitureItemPlacementData>(placement);

        await dbContext.SaveChangesAsync();
    }
}
