using Ada.API.DTOs;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Ada.API.Interfaces.Game.Rooms.Services.Wired;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Db.Models;
using Ada.Game.Mappers;
using Ada.Game.Rooms;
using Ada.Game.Rooms.Users;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ada.Tests.Game;

[TestFixture]
public class ServiceCollectionsTests
{
    [Test]
    public void RoomServiceCollection_AddServices_RegistersCoreServices()
    {
        var services = new ServiceCollection();

        RoomServiceCollection.AddServices(services);

        Assert.Multiple(() =>
        {
            var userRepository = services.Single(d => d.ServiceType == typeof(IRoomUserRepository));
            Assert.That(userRepository.ImplementationType, Is.EqualTo(typeof(RoomUserRepository)));
            Assert.That(userRepository.Lifetime, Is.EqualTo(ServiceLifetime.Transient));
            Assert.That(services.Single(d => d.ServiceType == typeof(IRoomRepository)).Lifetime,
                Is.EqualTo(ServiceLifetime.Singleton));
            Assert.That(services.Single(d => d.ServiceType == typeof(IRoomWiredService)).Lifetime,
                Is.EqualTo(ServiceLifetime.Transient));
        });
    }

    [Test]
    public void RoomServiceCollection_AddServices_RegistersDistinctWiredStrategies()
    {
        var services = new ServiceCollection();

        RoomServiceCollection.AddServices(services);

        var effects = services.Where(d => d.ServiceType == typeof(IWiredEffectStrategy)).ToList();
        var conditions = services.Where(d => d.ServiceType == typeof(IWiredConditionStrategy)).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(effects, Has.Count.EqualTo(11));
            Assert.That(conditions, Has.Count.EqualTo(16));
            Assert.That(effects.Select(d => d.ImplementationType).Distinct().Count(),
                Is.EqualTo(effects.Count));
            Assert.That(conditions.Select(d => d.ImplementationType).Distinct().Count(),
                Is.EqualTo(conditions.Count));
        });
    }

    [Test]
    public void MapperServiceCollection_AddServices_ResolvesWorkingMapper()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        MapperServiceCollection.AddServices(services);

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var dto = mapper.Map<BadgeDto>(new Badge { Id = 7, Code = "ADM" });

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(7));
            Assert.That(dto.Code, Is.EqualTo("ADM"));
        });
    }
}
