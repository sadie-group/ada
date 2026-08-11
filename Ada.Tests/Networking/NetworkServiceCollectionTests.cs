using Ada.API.Interfaces.Networking.Client;
using Ada.Networking;
using Ada.Networking.Client;
using Ada.Networking.Packets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NetworkOptions = Ada.Networking.Options.NetworkOptions;
using NetworkPacketOptions = Ada.Networking.Options.NetworkPacketOptions;

namespace Ada.Tests.Networking;

[TestFixture]
public class NetworkServiceCollectionTests
{
    private static IConfiguration CreateConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["NetworkOptions:Host"] = "127.0.0.1",
                ["NetworkOptions:Port"] = "30000",
                ["NetworkOptions:AllowInsecureTransport"] = "true",
                ["NetworkOptions:PacketOptions:BufferByteSize"] = "2048",
                ["NetworkOptions:PacketOptions:FrameLengthByteCount"] = "4",
                ["NetworkOptions:PacketOptions:NotifyMissingPacket"] = "true"
            })
            .Build();

    [Test]
    public void AddServices_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        NetworkServiceCollection.AddServices(services, CreateConfiguration());

        Assert.Multiple(() =>
        {
            Assert.That(services.Single(d => d.ServiceType == typeof(INetworkClientFactory)).ImplementationType,
                Is.EqualTo(typeof(NetworkClientFactory)));
            Assert.That(services.Single(d => d.ServiceType == typeof(INetworkClientRepository)).Lifetime,
                Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(services.Single(d => d.ServiceType == typeof(IClientDisposalService)).Lifetime,
                Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(services.Single(d => d.ServiceType == typeof(IPlayerSessionResumeService)).Lifetime,
                Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(services.Count(d => d.ServiceType == typeof(INetworkClient)),
                Is.GreaterThanOrEqualTo(1));
            Assert.That(services.Any(d => d.ServiceType == typeof(IHostedService)
                    && d.ImplementationType == typeof(NetworkListener)),
                Is.True);
            Assert.That(services.Single(d => d.ServiceType == typeof(IValidateOptions<NetworkOptions>)).ImplementationType?.Name,
                Is.EqualTo("NetworkOptionsValidator"));
            Assert.That(services.Single(d => d.ServiceType == typeof(IValidateOptions<NetworkPacketOptions>)).ImplementationType?.Name,
                Is.EqualTo("NetworkPacketOptionsValidator"));
        });
    }

    [Test]
    public void AddServices_BindsOptionsFromConfiguration()
    {
        var services = new ServiceCollection();
        NetworkServiceCollection.AddServices(services, CreateConfiguration());

        using var provider = services.BuildServiceProvider();
        var networkOptions = provider.GetRequiredService<IOptions<NetworkOptions>>().Value;
        var packetOptions = provider.GetRequiredService<IOptions<NetworkPacketOptions>>().Value;

        Assert.Multiple(() =>
        {
            Assert.That(networkOptions.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(networkOptions.Port, Is.EqualTo(30000));
            Assert.That(networkOptions.UseWss, Is.False);
            Assert.That(packetOptions.BufferByteSize, Is.EqualTo(2048));
            Assert.That(packetOptions.FrameLengthByteCount, Is.EqualTo(4));
            Assert.That(packetOptions.NotifyMissingPacket, Is.True);
        });
    }

    [Test]
    public void AddServices_ResolvesPacketHandlerFactory()
    {
        var services = new ServiceCollection();
        NetworkServiceCollection.AddServices(services, CreateConfiguration());

        using var provider = services.BuildServiceProvider();

        Assert.That(provider.GetRequiredService<PacketHandlerFactory>(), Is.Not.Null);
    }
}
