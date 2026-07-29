using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Db.Models.Players;
using Ada.Networking.Writers.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerRemoveUserIgnore)]
public class PlayerRemoveUserIgnoreEventHandler(IPlayerRepository playerRepository,
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public required string Username { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player?.NetworkObject == null)
        {
            return;
        }
        
        var targetPlayer = playerRepository.GetPlayerLogicByUsername(Username);
        
        if (targetPlayer == null || player.Player.OutgoingIgnores.All(x => x.TargetPlayerId != targetPlayer.Player.Id))
        {
            return;
        }

        var ignore = player.Player.OutgoingIgnores.FirstOrDefault(x => x.TargetPlayerId == targetPlayer.Player.Id);

        if (ignore == null)
        {
            return;
        }

        player.Player.OutgoingIgnores.Remove(ignore);

        await player.NetworkObject.WriteToStreamAsync(
            new PlayerIgnoreStateWriter
            {
                State = (int) PlayerIgnoreState.NotIgnored,
                Username = targetPlayer.Player.Username
            });
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        await dbContext.Set<PlayerIgnore>()
            .Where(x => x.PlayerId == player.Player.Id && x.TargetPlayerId == ignore.TargetPlayerId)
            .ExecuteDeleteAsync();
    }
}