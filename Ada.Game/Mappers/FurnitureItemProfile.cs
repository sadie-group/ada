using Ada.API.DTOs.Furniture;
using Ada.API.DTOs.Players.Furniture;
using Ada.Db.Models.Furniture;
using Ada.Db.Models.Players.Furniture;
using AutoMapper;

namespace Ada.Game.Mappers;

public class FurnitureItemProfile : Profile
{
    public FurnitureItemProfile()
    {
        CreateMap<FurnitureItem, FurnitureItemDto>().ReverseMap();
        
        CreateMap<PlayerFurnitureItem, PlayerFurnitureItemDto>()
            .ForMember(dest => dest.FurnitureItem, opt => opt.MapFrom(src => src.FurnitureItem));

        CreateMap<PlayerFurnitureItemDto, PlayerFurnitureItem>()
            .ForMember(dest => dest.FurnitureItem, opt => opt.Ignore())
            .ForMember(dest => dest.FurnitureItemId, opt => opt.MapFrom(src => src.FurnitureItem.Id));
        
        CreateMap<PlayerFurnitureItemPlacementData, PlayerFurnitureItemPlacementDataDto>()
            .ReverseMap();
    }
}