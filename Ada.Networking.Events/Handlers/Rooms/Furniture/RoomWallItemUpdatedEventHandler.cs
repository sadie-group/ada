using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Rooms.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players.Furniture;
using Ada.Networking.Writers.Rooms.Furniture;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomWallItemUpdated)]
public class RoomWallItemUpdatedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IMapper mapper,
    IPlayerRepository playerRepository)
    : INetworkPacketEventHandler
{
    public int ItemId { get; init; }
    public string WallPosition { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null || client.RoomUser == null)
        {
            return;
        }
        
        var room = roomRepository.TryGetRoomById(client.Player.State.CurrentRoomId);
        
        if (room == null || !client.RoomUser.HasRights())
        {
            return;
        }

        if (!client.RoomUser.HasRights())
        {
            await FurniturePlacementErrorSender.SendAsync(client, RoomFurniturePlacementError.MissingRights);
            return;
        }

        var roomFurnitureItem = room.Room.FurnitureItems.FirstOrDefault(x => x.PlayerFurnitureItem.Id == ItemId);

        if (roomFurnitureItem == null)
        {
            return;
        }

        var wallPosition = WallPosition;

        if (string.IsNullOrEmpty(wallPosition))
        {
            return;
        }

        roomFurnitureItem.WallPosition = wallPosition;
        
        var roomFurnitureItemEntity = mapper.Map<PlayerFurnitureItemPlacementData>(roomFurnitureItem);
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(roomFurnitureItemEntity).Property(x => x.WallPosition).IsModified = true;
        await dbContext.SaveChangesAsync();
        
        var ownerUsername = await playerRepository.GetPlayerUsernameByIdAsync(
            roomFurnitureItem.PlayerFurnitureItem.PlayerId);

        await room.BroadcastDataAsync(new RoomWallFurnitureItemUpdatedWriter
        {
            Item = roomFurnitureItem,
            OwnerUsername = ownerUsername ?? "Unknown User"
        });
    }
}