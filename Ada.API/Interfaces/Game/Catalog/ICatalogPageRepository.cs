using Ada.API.DTOs.Catalog.Pages;

namespace Ada.API.Interfaces.Game.Catalog;

public interface ICatalogPageRepository
{
    IReadOnlyList<CatalogPageDto> Pages { get; }
    Task LoadAsync();
}