using Ada.API.DTOs.Catalog.Pages;
using Ada.Core.Enums.Game.Catalog;

namespace Ada.API.DTOs.Catalog.FrontPage;

public class CatalogFrontPageItemDto
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? Image { get; init; }
    public CatalogFrontPageItemType TypeId { get; init; }
    public string? ProductName { get; init; }
    public int CatalogPageId { get; set; } 
    public CatalogPageDto? CatalogPage { get; init; }
}