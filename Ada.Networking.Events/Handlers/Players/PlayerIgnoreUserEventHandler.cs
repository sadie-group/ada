using Ada.API.DTOs.Players;
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

[PacketId(EventHandlerId.PlayerIgnoredUser)]
public class PlayerIgnoreUserEventHandler(IPlayerRepository playerRepository,
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
        
        if (targetPlayer == null || 
            player.Player.OutgoingIgnores.Any(x => x.TargetPlayerId == targetPlayer.Player.Id))
        {
            return;
        }

        var ignore = new PlayerIgnoreDto
        {
            PlayerId = player.Player.Id,
            TargetPlayerId = targetPlayer.Player.Id
        };

        player.Player.OutgoingIgnores.Add(ignore);

        await player.NetworkObject.WriteToStreamAsync(
            new PlayerIgnoreStateWriter
            {
                State = (int) PlayerIgnoreState.Ignored,
                Username = targetPlayer.Player.Username
            });
        
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        dbContext.Set<PlayerIgnore>().Add(new PlayerIgnore
        {
            PlayerId = player.Player.Id,
            TargetPlayerId = targetPlayer.Player.Id
        });

        await dbContext.SaveChangesAsync();
    }
}