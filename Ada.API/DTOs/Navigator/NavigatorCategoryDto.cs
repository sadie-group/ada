namespace Ada.API.DTOs.Navigator;

public class NavigatorCategoryDto
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string CodeName { get; init; }
    public required int OrderId { get; init; }
    public required int TabId { get; init; }
    public NavigatorTabDto? Tab { get; init; }
}