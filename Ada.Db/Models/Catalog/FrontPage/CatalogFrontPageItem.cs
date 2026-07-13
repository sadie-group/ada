using Ada.Core.Enums.Game.Catalog;
using Ada.Db.Models.Catalog.Pages;

namespace Ada.Db.Models.Catalog.FrontPage;

public class CatalogFrontPageItem
{
    public int Id { get; init; }
    public string? Title { get; init; }
    public string? Image { get; init; }
    public CatalogFrontPageItemType TypeId { get; init; }
    public string? ProductName { get; init; }
    public int CatalogPageId { get; set; } 
    public CatalogPage? CatalogPage { get; init; }
}