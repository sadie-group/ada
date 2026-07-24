using Ada.API.DTOs.Rooms;
using Ada.API.DTOs.Rooms.Chat;
using Ada.API.DTOs.Rooms.Rights;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Bots;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Db.Models.Rooms;
using Ada.Db.Models.Rooms.Chat;
using Ada.Db.Models.Rooms.Rights;
using Ada.Game.Rooms;
using Ada.Game.Rooms.Mapping;
using Ada.Game.Rooms.PathFinding;
using Ada.Game.Rooms.PathFinding.ToGo;
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
                var worldArray = tileMap.GetWorldArrayFromTileMap(tileMap, default, []);
                var worldGrid = new WorldGrid(worldArray);
                var pathFinder = new RoomPathFinder(worldGrid.Height, worldGrid.Width, new PathFinderOptions
                {
                    UseDiagonals = x.Settings?.WalkDiagonal ?? true
                });

                return new RoomLogic(
                        x,
                        tileMap,
                        pathFinder,
                        provider.GetRequiredService<IRoomUserRepository>(),
                        provider.GetRequiredService<IRoomBotRepository>())
                    {
                        Name = x.Name,
                        Description = x.Description
                    };
            });

        CreateMap<RoomLayout, RoomLayoutDto>().ReverseMap();
        CreateMap<Room, RoomDto>().ReverseMap();
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