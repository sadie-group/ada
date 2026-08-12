using Ada.API.DTOs.Catalog.Pages;
using Ada.API.Interfaces.Game.Players;

namespace Ada.Networking.Events.Handlers.Catalog;

public static class CatalogPageAccess
{
    public static bool CanAccess(CatalogPageDto? page, IPlayerLogic player)
    {
        if (page is not { Enabled: true, Visible: true })
        {
            return false;
        }

        if (page.RoleId == null)
        {
            return true;
        }

        return player.Player.Roles.Any(x => x.Id == page.RoleId);
    }
}
