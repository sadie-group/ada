using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Effects;

[PacketId(EventHandlerId.PlayerAvatarEffectSelected)]
public class PlayerAvatarEffectSelectedEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public int EffectId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var playerId = player.Player.Id;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var owned = await dbContext.PlayerEffectItems
            .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.EffectId == EffectId);

        if (owned == null || owned.IsActivated)
        {
            return;
        }

        owned.IsActivated = true;
        owned.ActivatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
    }
}
