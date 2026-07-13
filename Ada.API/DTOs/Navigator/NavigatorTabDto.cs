namespace Ada.API.DTOs.Navigator;

public class NavigatorTabDto
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public ICollection<NavigatorCategoryDto> Categories { get; init; } = [];
}