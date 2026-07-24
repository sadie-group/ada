using Ada.API.DTOs.Catalog.Items;
using Ada.API.DTOs.Catalog.Pages;
using Ada.Db.Models.Catalog.Items;
using Ada.Db.Models.Catalog.Pages;
using AutoMapper;

namespace Ada.Game.Mappers;

public class CatalogProfile : Profile
{
    public CatalogProfile()
    {
        CreateMap<CatalogPage, CatalogPageDto>();
        CreateMap<CatalogItem, CatalogItemDto>().ReverseMap();
    }
}