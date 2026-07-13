using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Generic;

namespace Ada.Networking.Events.Handlers.Generic;

[PacketId(EventHandlerId.PlayerRelationships)]
public class PlayerRelationshipsEventHandler(
    IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public int PlayerId { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var player = playerRepository.GetPlayerLogicById(PlayerId);

        var relationships = player != null ? 
                player.Player.OriginRelationships : 
                await playerRepository.GetRelationshipsForPlayerAsync(PlayerId);

        await client.WriteToStreamAsync(new PlayerRelationshipsWriter
        {
            PlayerId = PlayerId,
            Relationships = relationships
        });
    }
}