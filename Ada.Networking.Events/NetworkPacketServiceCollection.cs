using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Networking;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Client;
using Ada.Networking.Events.Handlers;
using Ada.Networking.Events.Handlers.Rooms;
using Ada.Networking.Packets;

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

        var packetHandlerTypeMap = new Dictionary<short, Type>();
        
        foreach(var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            var attributes = type.GetCustomAttributes(typeof(PacketIdAttribute), false);
            var headerAttribute = attributes.FirstOrDefault();

            if (headerAttribute == null)
            {
                continue;
            }
            
            packetHandlerTypeMap.Add(((PacketIdAttribute) headerAttribute).Id, type);
        }

        serviceCollection.AddSingleton(packetHandlerTypeMap);
        serviceCollection.AddSingleton<RoomHeightmapEventHandler>();
        serviceCollection.AddSingleton<INetworkPacketHandler, ClientPacketHandler>();
        serviceCollection.AddSingleton<INetworkPacketDecoder, NetworkPacketDecoder>();
        serviceCollection.AddSingleton<IWebSocketMessageReader, WebSocketMessageReader>();
        serviceCollection.AddSingleton<INetworkClientConnectionHandler, NetworkClientConnectionHandler>();
        serviceCollection.AddSingleton<PacketDispatcher>(p => new PacketDispatcher(
            p.GetRequiredService<INetworkPacketHandler>()));
    }
}