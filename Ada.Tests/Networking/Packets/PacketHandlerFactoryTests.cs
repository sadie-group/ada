using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Networking.Packets;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Tests.Networking.Packets;

[TestFixture]
public class PacketHandlerFactoryTests
{
    public interface IDependency;

    public class Dependency : IDependency;

    public class HandlerWithDependency(IDependency dependency) : INetworkPacketEventHandler
    {
        public IDependency Dependency { get; } = dependency;

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    public class ParameterlessHandler : INetworkPacketEventHandler
    {
        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    private static PacketHandlerFactory CreateFactory(Dictionary<short, Type> handlerTypes)
    {
        var provider = new ServiceCollection()
            .AddSingleton<IDependency, Dependency>()
            .BuildServiceProvider();

        return new PacketHandlerFactory(provider, handlerTypes);
    }

    [Test]
    public void Create_KnownId_ReturnsHandlerInstance()
    {
        var factory = CreateFactory(new Dictionary<short, Type> { [1] = typeof(ParameterlessHandler) });

        Assert.That(factory.Create(1), Is.InstanceOf<ParameterlessHandler>());
    }

    [Test]
    public void Create_HandlerWithConstructorDependency_ResolvesFromProvider()
    {
        var factory = CreateFactory(new Dictionary<short, Type> { [2] = typeof(HandlerWithDependency) });

        var handler = (HandlerWithDependency)factory.Create(2);

        Assert.That(handler.Dependency, Is.InstanceOf<Dependency>());
    }

    [Test]
    public void Create_ReturnsNewInstancePerCall()
    {
        var factory = CreateFactory(new Dictionary<short, Type> { [1] = typeof(ParameterlessHandler) });

        Assert.That(factory.Create(1), Is.Not.SameAs(factory.Create(1)));
    }

    [Test]
    public void Create_UnknownId_ReturnsNull()
    {
        var factory = CreateFactory([]);

        Assert.That(factory.Create(99), Is.Null);
    }
}
