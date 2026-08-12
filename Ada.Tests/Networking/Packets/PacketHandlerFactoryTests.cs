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

    private static PacketHandlerFactory CreateFactory()
    {
        var provider = new ServiceCollection()
            .AddSingleton<IDependency, Dependency>()
            .BuildServiceProvider();

        return new PacketHandlerFactory(provider);
    }

    [Test]
    public void Create_KnownType_ReturnsHandlerInstance()
    {
        var factory = CreateFactory();

        Assert.That(factory.Create(typeof(ParameterlessHandler)), Is.InstanceOf<ParameterlessHandler>());
    }

    [Test]
    public void Create_HandlerWithConstructorDependency_ResolvesFromProvider()
    {
        var factory = CreateFactory();

        var handler = (HandlerWithDependency)factory.Create(typeof(HandlerWithDependency));

        Assert.That(handler.Dependency, Is.InstanceOf<Dependency>());
    }

    [Test]
    public void Create_ReturnsNewInstancePerCall()
    {
        var factory = CreateFactory();

        Assert.That(
            factory.Create(typeof(ParameterlessHandler)),
            Is.Not.SameAs(factory.Create(typeof(ParameterlessHandler))));
    }

    public interface IUnregisteredDependency;

    public class HandlerWithUnregisteredDependency(IUnregisteredDependency dependency) : INetworkPacketEventHandler
    {
        public IUnregisteredDependency Dependency { get; } = dependency;

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    public class HandlerWithTwoConstructors : INetworkPacketEventHandler
    {
        public IDependency? Dependency { get; }

        public HandlerWithTwoConstructors()
        {
        }

        public HandlerWithTwoConstructors(IDependency dependency)
        {
            Dependency = dependency;
        }

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    public class HandlerWithMarkedConstructor : INetworkPacketEventHandler
    {
        public IDependency? Dependency { get; }

        public HandlerWithMarkedConstructor()
        {
        }

        [ActivatorUtilitiesConstructor]
        public HandlerWithMarkedConstructor(IDependency dependency)
        {
            Dependency = dependency;
        }

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
    }

    [Test]
    public void Create_MissingDependency_ThrowsNamingTheService()
    {
        var factory = CreateFactory();

        var exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(typeof(HandlerWithUnregisteredDependency)));

        Assert.That(exception!.Message, Does.Contain(nameof(IUnregisteredDependency)));
    }

    [Test]
    public void Create_AmbiguousConstructors_ThrowsNamingTheHandler()
    {
        var factory = CreateFactory();

        var exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(typeof(HandlerWithTwoConstructors)));

        Assert.That(exception!.Message, Does.Contain(nameof(HandlerWithTwoConstructors)));
    }

    [Test]
    public void Create_MarkedConstructor_ResolvesTheMarkedOne()
    {
        var factory = CreateFactory();

        var handler = (HandlerWithMarkedConstructor) factory.Create(typeof(HandlerWithMarkedConstructor));

        Assert.That(handler.Dependency, Is.InstanceOf<Dependency>());
    }

    [Test]
    public void Create_TypeNotSeenBefore_BuildsFactoryOnDemand()
    {
        var factory = CreateFactory();
        Assert.Multiple(() =>
        {
            Assert.That(factory.Create(typeof(HandlerWithDependency)), Is.InstanceOf<HandlerWithDependency>());
            Assert.That(factory.Create(typeof(ParameterlessHandler)), Is.InstanceOf<ParameterlessHandler>());
        });
    }
}
