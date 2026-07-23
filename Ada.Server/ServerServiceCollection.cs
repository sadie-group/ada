using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ada.API;
using Ada.API.Interfaces.Networking.Events.Filters;
using Ada.API.Interfaces.Plugins;
using Ada.API.Interfaces.Server;
using Ada.API.Interfaces.Server.Tasks;
using Ada.Db;
using Ada.Db.Models.Server;
using Ada.Game;
using Ada.Game.Catalog;
using Ada.Game.Groups;
using Ada.Game.Locale;
using Ada.Game.Mappers;
using Ada.Game.Navigator;
using Ada.Game.Players;
using Ada.Game.Rooms;
using Ada.Game.WordFilter;
using Ada.Networking;
using Ada.Networking.Encryption;
using Ada.Networking.Events;
using Ada.Server.Infrastructure;
using Ada.Server.Tasks;

namespace Ada.Server;

public static class ServerServiceCollection
{
    public static void AddServices(IServiceCollection services, IConfiguration config)
    {
        services.AddOptions();
        
        services.AddSingleton<IServer, Server>();
        services.AddSingleton<IServerMigrator, ServerMigrator>();
        services.AddSingleton<IServerDataCleaner, ServerDataCleaner>();
        services.AddSingleton<IServerTaskWorker, ServerTaskWorker>();

        MapperServiceCollection.AddServices(services);
        DatabaseServiceCollection.AddServices(services, config);
        NetworkServiceCollection.AddServices(services, config);
        NetworkPacketServiceCollection.AddServices(services);

        services.AddSingleton(new ServerSettings());
        services.AddSingleton(new List<ServerPeriodicCurrencyReward>());
        services.AddHostedService<GameWorker>();
        
        RegisterGameServices(services, config);

        ServiceCollectionHelpers.LoadPlugins(config);
        
        RegisterReflectionDiscoveredServices(services);
    }
    
    private static void RegisterGameServices(IServiceCollection services, IConfiguration config)
    {
        PlayerServiceCollection.AddServices(services, config);
        RoomServiceCollection.AddServices(services);
        NavigatorServiceCollection.AddServices(services);
        EncryptionServiceProvider.AddServices(services, config);
        LocaleServiceCollection.AddServices(services);
        CatalogServiceCollection.AddServices(services, config);
        GroupServiceCollection.AddServices(services);
        WordFilterServiceCollection.AddServices(services);
    }
    
    private static void RegisterReflectionDiscoveredServices(IServiceCollection services)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();

        services.RegisterRoomChatCommands(assemblies);
        services.RegisterFurnitureInteractors(assemblies);
        services.RegisterRoomFurnitureProcessors(assemblies);
        services.RegisterPluginServices(assemblies);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<IServerTask>())
            .AsImplementedInterfaces()
            .WithSingletonLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<IPlayerSessionListener>())
            .As<IPlayerSessionListener>()
            .WithSingletonLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(c => c.AssignableTo<INetworkPacketEventFilter>())
            .As<INetworkPacketEventFilter>()
            .WithSingletonLifetime());
    }
}