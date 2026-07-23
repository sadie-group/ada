using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Pathfinding;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Game.Rooms.Bots;
using Ada.Game.Rooms.Pets;
using Ada.Game.Rooms.Chat.Commands;
using Ada.Game.Rooms.Furniture;
using Ada.Game.Rooms.Mapping;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.Wired;
using Ada.Game.Rooms.Wired.Conditions;
using Ada.Game.Rooms.Wired.Effects;
using Ada.Game.Rooms.Services;
using Ada.Game.Rooms.Users;

namespace Ada.Game.Rooms;

public static class RoomServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddTransient<IRoomUserRepository, RoomUserRepository>();
        serviceCollection.AddTransient<IRoomBotRepository, RoomBotRepository>();
        serviceCollection.AddTransient<IRoomPetRepository, RoomPetRepository>();
        serviceCollection.AddSingleton<IRoomUserFactory, RoomUserFactory>();
        serviceCollection.AddSingleton<IRoomBotFactory, RoomBotFactory>();
        serviceCollection.AddSingleton<IRoomPetFactory, RoomPetFactory>();
        serviceCollection.AddSingleton<IRoomRepository, RoomRepository>();

        serviceCollection.AddSingleton<IRoomChatCommandRepository, RoomChatCommandRepository>();
        serviceCollection.AddSingleton<IRoomFurnitureItemInteractorRepository, RoomFurnitureItemInteractorRepository>();
        serviceCollection.AddTransient<IRoomWiredService, RoomWiredService>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectShowMessageStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectKickUserStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectToggleFurnitureStateStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectTeleportUserStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionUserCountStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotUserCountStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTriggererOnFurnitureStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotTriggererOnFurnitureStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionFurnitureHasUsersStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotFurnitureHasUsersStrategy>();
        serviceCollection.AddSingleton<IWiredTimerService, WiredTimerService>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectResetTimersStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectMuteTriggererStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectCallAnotherStackStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectMoveRotateStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectChaseStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectFleeStrategy>();
        serviceCollection.AddSingleton<IWiredEffectStrategy, WiredEffectMoveToDirectionStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTimeElapsedMoreStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTimeElapsedLessStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionFurnitureHasFurnitureStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotFurnitureHasFurnitureStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTriggererWearsBadgeStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotTriggererWearsBadgeStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTriggererWearsEffectStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionNotTriggererWearsEffectStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionTriggererHasHandItemStrategy>();
        serviceCollection.AddSingleton<IWiredConditionStrategy, WiredConditionDateRangeActiveStrategy>();
        serviceCollection.AddSingleton<IRoomTileMapHelperService, RoomTileMapHelperService>();
        serviceCollection.AddSingleton<IRoomHelperService, RoomHelperService>();
        serviceCollection.AddSingleton<IRoomFloodProtectionService, RoomFloodProtectionService>();
        serviceCollection.AddSingleton<IRoomFurnitureItemHelperService, RoomFurnitureItemHelperService>();
        serviceCollection.AddSingleton<IRoomPathFinderHelperService, RoomPathFinderHelperService>();
    }
}