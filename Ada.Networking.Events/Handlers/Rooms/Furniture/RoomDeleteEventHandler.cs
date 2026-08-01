using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using AutoMapper;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDelete)]
public class RoomDeleteEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper,
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public required int RoomId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var room = await RoomHelpers.TryLoadRoomByIdAsync(
            RoomId, 
            roomRepository, 
            dbContextFactory, 
            mapper);

        if (room == null || room.Room.OwnerId != client.Player.Player.Id)
        {
            return;
        }

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.ExecuteSqlRawAsync("UPDATE player_data SET home_room_id = NULL WHERE home_room_id = {0}", RoomId);

        var updateMap = new Dictionary<IPlayerLogic, List<PlayerFurnitureItemDto>>();

        foreach (var item in room.Room.FurnitureItems)
        {
            var playerItem = item.PlayerFurnitureItem;
            playerItem.PlacementData = null;

            var onlineOwner = playerRepository.GetPlayerLogicById(item.PlayerFurnitureItem.PlayerId);

            if (onlineOwner == null)
            {
                continue;
            }
            
            if (!updateMap.ContainsKey(onlineOwner))
            {
                updateMap[onlineOwner] = [];
            }

            updateMap[onlineOwner].Add(item.PlayerFurnitureItem);
        }

        if (!roomRepository.TryRemove(RoomId, out _))
        {
            return;
        }

        await dbContext.RoomFurnitureItems
            .Where(x => x.RoomId == room.Room.Id)
            .ExecuteDeleteAsync();

        await dbContext.Rooms
            .Where(x => x.Id == room.Room.Id)
            .ExecuteDeleteAsync();

        foreach (var roomUser in room.UserRepository.GetAll())
        {
            await room.UserRepository.TryRemoveAsync(roomUser.Player.Player.Id);
        }
    }
}