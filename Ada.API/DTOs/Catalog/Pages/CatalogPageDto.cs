using Ada.API.DTOs.Catalog.Items;

namespace Ada.API.DTOs.Catalog.Pages;

public record CatalogPageDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public string? Caption { get; init; }
    public string? Layout { get; init; }
    public int? RoleId { get; init; }
    public int? CatalogPageId { get; init; }
    public int OrderId { get; init; }
    public int IconId { get; init; }
    public bool Enabled { get; init; }
    public bool Visible { get; init; }
    public List<string> ImagesJson { get; init; } = [];
    public List<string> TextsJson { get; init; } = [];
    public ICollection<CatalogPageDto> Pages { get; init; } = [];
    public ICollection<CatalogItemDto> Items { get; init; } = [];
}