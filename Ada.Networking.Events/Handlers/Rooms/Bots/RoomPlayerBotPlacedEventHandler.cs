using System.Drawing;
using Microsoft.EntityFrameworkCore;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Inventory;
using Ada.Networking.Writers.Rooms;
using Ada.Networking.Writers.Rooms.Bots;

namespace Ada.Networking.Events.Handlers.Rooms.Bots;

[PacketId(EventHandlerId.RoomPlayerBotPlaced)]
public class RoomPlayerBotPlacedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository,
    IRoomBotFactory roomBotFactory) : INetworkPacketEventHandler
{
    public required int Id { get; init; }
    public required int X { get; init; }
    public required int Y { get; init; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (!RoomContextResolver.TryResolveRoomObjectsForClient(roomRepository,
                client,
                out var room,
                out var roomUser))
        {
            return;
        }

        var bot = client
            .Player!
            .Player.Bots
            .FirstOrDefault(x => x.Id == Id);

        if (bot == null)
        {
            return;
        }

        if (room.Room.OwnerId != roomUser.Player.Player.Id)
        {
            return;
        }

        var placePoint = new Point(X, Y);
        
        if (room.TileMap.UsersAtPoint(placePoint) && !room.Room.Settings.CanUsersOverlap)
        {
            await client.WriteToStreamAsync(new RoomBotErrorWriter
            {
                ErrorCode = 3
            });
            
            return;
        }

        var roomBot = roomBotFactory.Create(
            room, 
            room.Room.MaxUsersAllowed + bot.Id, 
            new Point(X, Y), 
            room.TileMap.ZMap[Y, X]);

        if (!room.BotRepository.TryAdd(roomBot))
        {
            return;
        }

        bot.RoomId = room.Room.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.Entry(bot).Property(x => x.RoomId).IsModified = true;
        await dbContext.SaveChangesAsync();

        room.TileMap.AddUnitToMap(new Point(X, Y), roomBot);
        
        await room.BroadcastDataAsync(new RoomBotDataWriter
        {
            Bots = [roomBot]
        });

        await room.BroadcastDataAsync(new RoomBotStatusWriter
        {
            Bots = [roomBot]
        });
        
        await client.WriteToStreamAsync(new PlayerInventoryRemoveBotWriter
        {
            Id = bot.Id
        });
    }
}