using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.API.Interfaces.Networking;
using Ada.Db.Models.Rooms;
using Ada.Networking.Packets.Serialization;

namespace Ada.Game.Rooms;

public class RoomLogic(
    RoomDto room,
    IRoomTileMap tileMap,
    IRoomPathFinder pathFinder,
    IRoomUserRepository userRepository,
    IRoomBotRepository botRepository,
    IRoomPetRepository petRepository)
    : Room, IRoomLogic
{
    public RoomDto Room { get; } = room;
    public IRoomTileMap TileMap { get; } = tileMap;
    public IRoomPathFinder PathFinder { get; } = pathFinder;
    public IRoomUserRepository UserRepository { get; } = userRepository;
    public IRoomBotRepository BotRepository { get; } = botRepository;
    public IRoomPetRepository PetRepository { get; } = petRepository;
    
    public async ValueTask DisposeAsync()
    {
    }
    
    public async Task BroadcastDataAsync(AbstractPacketWriter writer, IReadOnlyCollection<long>? excludedIds = null)
    {
        var packet = NetworkPacketWriterSerializer.Serialize(writer);

        var excluded = excludedIds is { Count: > 0 }
            ? excludedIds as IReadOnlySet<long> ?? new HashSet<long>(excludedIds)
            : null;

        var sendTasks = new List<Task>();

        foreach (var user in UserRepository.GetAll())
        {
            if (excluded != null && excluded.Contains(user.Player.Player.Id))
            {
                continue;
            }

            sendTasks.Add(user.NetworkObject.WriteToStreamAsync(packet));
        }

        await Task.WhenAll(sendTasks);
    }
}