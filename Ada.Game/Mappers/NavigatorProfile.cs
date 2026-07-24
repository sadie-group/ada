using Ada.API.DTOs.Navigator;
using Ada.Db.Models.Navigator;
using AutoMapper;

namespace Ada.Game.Mappers;

public class NavigatorProfile : Profile
{
    public NavigatorProfile()
    {
        CreateMap<NavigatorCategory, NavigatorCategoryDto>();
        CreateMap<NavigatorTab, NavigatorTabDto>();
    }
}