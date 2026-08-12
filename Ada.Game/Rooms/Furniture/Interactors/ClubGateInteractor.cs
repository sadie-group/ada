using Ada.API.DTOs.Players.Furniture;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Users;
using Ada.Core.Enums.Game.Furniture;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Game.Rooms.Furniture.Interactors;

public class ClubGateInteractor(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomFurnitureItemHelperService roomFurnitureItemHelperService)
    : GateInteractor(dbContextFactory, roomFurnitureItemHelperService)
{
    public override List<string> InteractionTypes => [FurnitureItemInteractionType.ClubGate];

    public override async Task OnTriggerAsync(IRoomLogic room, PlayerFurnitureItemPlacementDataDto item, IRoomUser roomUser)
    {
        var hasActiveClub = roomUser.Player.Player.Subscriptions.Any(x =>
            x.Subscription?.Name == "HABBO_CLUB" &&
            x.ExpiresAt > DateTimeOffset.Now);

        if (!hasActiveClub)
        {
            return;
        }

        await base.OnTriggerAsync(room, item, roomUser);
    }
}
