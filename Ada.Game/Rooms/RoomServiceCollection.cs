using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms.Bots;
using Ada.Game.Rooms.Chat.Commands;
using Ada.Game.Rooms.Furniture;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Services;
using Ada.Game.Rooms.Users;

namespace Ada.Game.Rooms;

public static class RoomServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IRoomUserRepository, RoomUserRepository>();
        serviceCollection.AddTransient<IRoomBotRepository, RoomBotRepository>();
        serviceCollection.AddSingleton<IRoomUserFactory, RoomUserFactory>();
        serviceCollection.AddSingleton<IRoomBotFactory, RoomBotFactory>();
        serviceCollection.AddSingleton<IRoomRepository, RoomRepository>();

        serviceCollection.AddSingleton<IRoomChatCommandRepository, RoomChatCommandRepository>();
        serviceCollection.AddSingleton<IRoomFurnitureItemInteractorRepository, RoomFurnitureItemInteractorRepository>();
        serviceCollection.AddTransient<IRoomWiredService, RoomWiredService>();
        serviceCollection.AddSingleton<IRoomTileMapHelperService, RoomTileMapHelperService>();
        serviceCollection.AddSingleton<IRoomHelperService, RoomHelperService>();
        serviceCollection.AddSingleton<IRoomFloodProtectionService, RoomFloodProtectionService>();
        serviceCollection.AddSingleton<IRoomFurnitureItemHelperService, RoomFurnitureItemHelperService>();
        serviceCollection.AddSingleton<IRoomPathFinderHelperService, RoomPathFinderHelperService>();
    }
}