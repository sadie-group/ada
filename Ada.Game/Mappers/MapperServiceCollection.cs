using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ada.API.DTOs;
using Ada.API.DTOs.Server;
using Ada.Db.Models;
using Ada.Db.Models.Server;

namespace Ada.Game.Mappers;

public static class MapperServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection)
    {
        var profiles = new[]
        {
            typeof(RoomProfile),
            typeof(PlayerProfile),
            typeof(NavigatorProfile),
            typeof(CatalogProfile),
            typeof(FurnitureItemProfile)
        };
        
        foreach (var profile in profiles)
        {
            serviceCollection.AddSingleton(profile);
        }

        serviceCollection.AddSingleton(provider => new MapperConfiguration(c =>
        {
            foreach (var profile in profiles)
            {
                c.AddProfile(provider.GetRequiredService(profile) as Profile);
            }
            
            c.CreateMap<Group, GroupDto>();
            c.CreateMap<Role, RoleDto>();
            c.CreateMap<Permission, PermissionDto>();
            c.CreateMap<Badge, BadgeDto>();
            c.CreateMap<HandItem, HandItemDto>();
            c.CreateMap<ServerPeriodicCurrencyRewardLog, ServerPeriodicCurrencyRewardLogDto>();
            c.CreateMap<Subscription, SubscriptionDto>();

            c.ShouldMapProperty = p => p.GetIndexParameters().Length == 0;
        }, provider.GetRequiredService<ILoggerFactory>()).CreateMapper());
    }
}