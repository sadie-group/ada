using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Client;
using Ada.Networking.Packets;
using Ada.Networking.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NetworkOptions = Ada.Networking.Options.NetworkOptions;
using NetworkPacketOptions = Ada.Networking.Options.NetworkPacketOptions;

namespace Ada.Networking;

public static class NetworkServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        serviceCollection.AddSingleton<INetworkClientFactory, NetworkClientFactory>();
        serviceCollection.AddSingleton<INetworkClientRepository, NetworkClientRepository>();
        serviceCollection.AddSingleton<IClientDisposalService, ClientDisposalService>();
        serviceCollection.AddSingleton<IPlayerSessionResumeService, PlayerSessionResumeService>();
        serviceCollection.AddSingleton<ILoginAttemptThrottle, LoginAttemptThrottle>();
        serviceCollection.AddSingleton<IRoomAccessThrottle, RoomAccessThrottle>();

        serviceCollection.AddTransient<INetworkClient, NetworkClient>();

        serviceCollection.AddHostedService<NetworkListener>();
        
        serviceCollection.Configure<NetworkOptions>(options => config.GetSection("NetworkOptions").Bind(options));
        serviceCollection.Configure<NetworkPacketOptions>(options => config.GetSection("NetworkOptions:PacketOptions").Bind(options));

        serviceCollection.AddSingleton<IValidateOptions<NetworkOptions>, NetworkOptionsValidator>();
        serviceCollection.AddSingleton<IValidateOptions<NetworkPacketOptions>, NetworkPacketOptionsValidator>();
        
        serviceCollection.AddSingleton<PacketHandlerFactory>(sp => new PacketHandlerFactory(sp));
    }
}