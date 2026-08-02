using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking.Client;
using Ada.Networking.Events.Handlers;
using Ada.Networking.Events.Handlers.Rooms;
using Ada.Networking.Packets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ada.Networking.Events;

public static class NetworkPacketServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        serviceCollection.Scan(scan => scan
            .FromAssemblyOf<INetworkPacketEventHandler>()
            .AddClasses(classes => classes.AssignableTo<INetworkPacketEventHandler>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        serviceCollection.AddSingleton<RoomHeightmapEventHandler>();
        serviceCollection.AddSingleton<PlayerLoginPacketService>();
        serviceCollection.AddSingleton<INetworkPacketHandler, ClientPacketHandler>();
        serviceCollection.AddSingleton<INetworkPacketDecoder, NetworkPacketDecoder>();
        serviceCollection.AddSingleton<IPacketIdMap>(_ => new DefaultPacketIdMap());

        serviceCollection.AddSingleton<IPacketCodec>(sp => new BinaryPacketCodec(
            sp.GetRequiredService<IPacketIdMap>(),
            sp.GetRequiredService<INetworkPacketDecoder>()));

        serviceCollection.AddSingleton<IPacketCodecRegistry>(sp => new PacketCodecRegistry(
            sp.GetServices<IPacketCodec>(),
            BinaryPacketCodec.RevisionName));
        serviceCollection.AddSingleton<IWebSocketMessageReader, WebSocketMessageReader>();
        serviceCollection.AddSingleton<INetworkClientConnectionHandler, NetworkClientConnectionHandler>();
        serviceCollection.AddSingleton<PacketDispatcher>(p => new PacketDispatcher(
            p.GetRequiredService<INetworkPacketHandler>(),
            p.GetRequiredService<ILogger<PacketDispatcher>>()));
    }
}