using Ada.API.DTOs.Players.Furniture;
using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Rooms.Chat;
using Ada.API.DTOs.Rooms.Rights;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Pets;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Db.Models.Rooms;
using Ada.Db.Models.Rooms.Chat;
using Ada.Db.Models.Rooms.Rights;
using Ada.Game.Rooms;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.PathFinding.ToGo.Options;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Game.Mappers;

public class RoomProfile : Profile
{
    public RoomProfile(IServiceProvider provider)
    {
        CreateMap<RoomDto, IRoomLogic>()
            .ConstructUsing((x, _) =>
            {
                var tileMap = new RoomTileMap(x.Layout!.Heightmap ?? "", x.FurnitureItems);
                var pathFinder = new RoomPathFinder(tileMap.SizeY, tileMap.SizeX, new PathFinderOptions
                {
                    UseDiagonals = x.Settings?.WalkDiagonal ?? true
                });

                return new RoomLogic(
                        x,
                        tileMap,
                        pathFinder,
                        provider.GetRequiredService<IRoomUserRepository>(),
                        provider.GetRequiredService<IRoomBotRepository>(),
                        provider.GetRequiredService<IRoomPetRepository>(),
                        provider.GetRequiredService<IRoomLockFactory>().Create(x.Id))
                    {
                        Name = x.Name,
                        Description = x.Description
                    };
            });

        CreateMap<RoomLayout, RoomLayoutDto>().ReverseMap();

        CreateMap<Room, RoomDto>()
            .ForMember(x => x.FurnitureItems, o => o.Ignore())
            .AfterMap((src, dest, context) =>
                dest.FurnitureItems.AddRange(
                    context.Mapper.Map<List<PlayerFurnitureItemPlacementDataDto>>(src.FurnitureItems)))
            .ReverseMap()
            .ForMember(x => x.FurnitureItems, o => o.MapFrom(x => x.FurnitureItems));
        CreateMap<RoomChatSettings, RoomChatSettingsDto>().ReverseMap();
        CreateMap<RoomChatMessage, RoomChatMessageDto>().ReverseMap();
        CreateMap<List<RoomChatMessage>, List<RoomChatMessageDto>>().ReverseMap();
        CreateMap<RoomPaintSettings, RoomPaintSettingsDto>().ReverseMap();
        CreateMap<RoomSettings, RoomSettingsDto>().ReverseMap();
        CreateMap<RoomPlayerRight,  RoomPlayerRightDto>();
        CreateMap<RoomTag, RoomTagDto>();
        CreateMap<RoomDimmerSettings, RoomDimmerSettingsDto>();
    }
}
