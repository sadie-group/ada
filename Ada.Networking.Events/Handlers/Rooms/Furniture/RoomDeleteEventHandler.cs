using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Game.Rooms;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RoomDelete)]
public class RoomDeleteEventHandler(
    IRoomRepository roomRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IMapper mapper,
    IPlayerRepository playerRepository,
    IPlayerHelperService playerHelperService,
    ILogger<RoomDeleteEventHandler> logger) : INetworkPacketEventHandler, IDefersPersistence
{
    public required int RoomId { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var room = await RoomHelpers.TryLoadRoomByIdAsync(
            RoomId, 
            roomRepository, 
            dbContextFactory, 
            mapper);

        if (room == null || room.Room.OwnerId != client.Player.Player.Id)
        {
            return;
        }


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

        foreach (var roomUser in room.UserRepository.GetAll())
        {
            await room.UserRepository.TryRemoveAsync(roomUser.Player.Player.Id, true, true);
        }

        var roomId = room.Room.Id;

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            await dbContext.PlayerData
                .Where(x => x.HomeRoomId == RoomId)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.HomeRoomId, (int?) null));

            await dbContext.RoomFurnitureItems
                .Where(x => x.RoomId == roomId)
                .ExecuteDeleteAsync();

            await dbContext.Rooms
                .Where(x => x.Id == roomId)
                .ExecuteDeleteAsync();
        };

        foreach (var (owner, items) in updateMap)
        {
            try
            {
                await playerHelperService.SendUnseenInventoryItemsAsync(owner, items);
                await playerHelperService.RefreshInventoryAsync(owner);
            }
            catch (Exception e)
            {
                logger.LogError(e,
                    "Failed to refresh inventory for player {PlayerId} after room {RoomId} was deleted",
                    owner.Player.Id, RoomId);
            }
        }
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
