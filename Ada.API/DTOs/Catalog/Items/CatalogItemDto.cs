using Ada.API.DTOs.Furniture;

namespace Ada.API.DTOs.Catalog.Items;

public record CatalogItemDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public int CostCredits { get; init; }
    public int CostPoints { get; init; }
    public int CostPointsType { get; init; }
    public ICollection<FurnitureItemDto> FurnitureItems { get; init; } = [];
    public bool RequiresClubMembership { get; init; }
    public string? MetaData { get; init; }
    public int Amount { get; init; }
    public int StackLimit { get; init; }
    public int SellLimit { get; init; }
}