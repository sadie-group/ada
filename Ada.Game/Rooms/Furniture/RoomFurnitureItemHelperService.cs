using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Enums.Miscellaneous;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Furniture;

public class RoomFurnitureItemHelperService(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IPlayerRepository playerRepository,
    IMapper mapper) : IRoomFurnitureItemHelperService
{
    public async Task CycleInteractionStateForItemAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem)
    {
        if (string.IsNullOrEmpty(roomFurnitureItem.PlayerFurnitureItem.MetaData))
        {
            roomFurnitureItem.PlayerFurnitureItem.MetaData = 0.ToString();
        }

        var furnitureItem = roomFurnitureItem.PlayerFurnitureItem.FurnitureItem;
        
        if (furnitureItem.InteractionModes < 1 ||
            !int.TryParse(roomFurnitureItem.PlayerFurnitureItem.MetaData, out var state))
        {
            return;
        }

        if (state >= furnitureItem.InteractionModes)
        {
            state = 0;
        }

        await UpdateMetaDataForItemAsync(room, roomFurnitureItem, (state + 1).ToString());
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        
        var playerFurnitureItemEntity = mapper.Map<PlayerFurnitureItem>(roomFurnitureItem.PlayerFurnitureItem);
        
        dbContext
            .Entry(playerFurnitureItemEntity)
            .Property(x => x.MetaData).IsModified = true;
        
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateMetaDataForItemAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem, 
        string metaData)
    {
        roomFurnitureItem.PlayerFurnitureItem.MetaData = metaData;
        await BroadcastItemUpdateToRoomAsync(room, roomFurnitureItem);
    }

    public async Task BroadcastItemUpdateToRoomAsync(
        IRoomLogic room, 
        PlayerFurnitureItemPlacementDataDto roomFurnitureItem)
    {
        var furnitureItem = roomFurnitureItem.PlayerFurnitureItem.FurnitureItem;

        AbstractPacketWriter itemWriter = furnitureItem.Type == FurnitureItemType.Floor ? 
            new RoomFloorItemUpdatedWriter
            {
                Id = roomFurnitureItem.PlayerFurnitureItemId,
                AssetId = furnitureItem.AssetId,
                PositionX = roomFurnitureItem.PositionX,
                PositionY = roomFurnitureItem.PositionY,
                Direction = (int)roomFurnitureItem.Direction,
                PositionZ = roomFurnitureItem.PositionZ,
                StackHeight = 0.ToString(),
                Extra = 0,
                ObjectDataKey = (int) GetObjectDataKeyForItem(roomFurnitureItem),
                ObjectData = GetObjectDataForItem(roomFurnitureItem),
                MetaData = roomFurnitureItem.PlayerFurnitureItem.MetaData,
                Expires = -1,
                InteractionModes = 1,
                OwnerId = roomFurnitureItem.PlayerFurnitureItem.PlayerId
            }
            : new RoomWallFurnitureItemUpdatedWriter
        {
            Item = roomFurnitureItem,
            OwnerUsername = await playerRepository.GetPlayerUsernameByIdAsync(
                roomFurnitureItem.PlayerFurnitureItem.PlayerId
            ) ?? "Unknown User"
        };
        
        await room.BroadcastDataAsync(itemWriter);
    }

    public ObjectDataKey GetObjectDataKeyForItem(PlayerFurnitureItemPlacementDataDto furnitureItem)
    {
        return furnitureItem.PlayerFurnitureItem.FurnitureItem.InteractionType switch
        {
            FurnitureItemInteractionType.RoomAdsBg => ObjectDataKey.MapKey,
            _ => ObjectDataKey.LegacyKey
        };
    }

    public Dictionary<string, string> GetObjectDataForItem(PlayerFurnitureItemPlacementDataDto furnitureItem)
    {
        if (furnitureItem.PlayerFurnitureItem.FurnitureItem.InteractionType != FurnitureItemInteractionType.RoomAdsBg)
        {
            return new Dictionary<string, string>();
        }
        
        var data = new Dictionary<string, string>();
            
        foreach (var piece in furnitureItem.PlayerFurnitureItem.MetaData.Split(";"))
        {
            var parts = piece.Split("=");
            var key = parts[0];
            var value = parts.Length < 2 ? "" : parts[1];

            data[key] = value;
        }

        return data;
    }
}