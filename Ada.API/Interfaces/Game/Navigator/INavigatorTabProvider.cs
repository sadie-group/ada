using Ada.API.DTOs.Navigator;

namespace Ada.API.Interfaces.Game.Navigator;

public interface INavigatorTabProvider
{
    Task<IReadOnlyList<NavigatorCategoryDto>> GetCategoriesForTabAsync(string? tabName);

    void Invalidate();
}
