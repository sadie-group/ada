using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Messenger;
using Ada.Networking.Writers.Rooms.Users;

namespace Ada.Networking.Events.Handlers.Players.Messenger;

[PacketId(EventHandlerId.PlayerStalk)]
public class PlayerStalkEventHandler(IPlayerRepository playerRepository) : INetworkPacketEventHandler
{
    public int PlayerId { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var playerId = PlayerId;

        if (!client.Player.IsFriendsWith(PlayerId))
        {
            await client.WriteToStreamAsync(new PlayerStalkErrorWriter
            {
                StalkError = (int) PlayerStalkError.NotFriends
            });
            
            return;
        }

        var targetPlayer = playerRepository.GetPlayerLogicById(playerId);
        
        if (targetPlayer == null)
        {
            await client.WriteToStreamAsync(new PlayerStalkErrorWriter
            {
                StalkError = (int) PlayerStalkError.TargetOffline
            });
            
            return;
        }

        if (targetPlayer.State.CurrentRoomId == 0)
        {
            await client.WriteToStreamAsync(new PlayerStalkErrorWriter
            {
                StalkError = (int) PlayerStalkError.TargetNotInRoom
            });
            
            return;
        }

        if (client.Player.State.CurrentRoomId == targetPlayer.State.CurrentRoomId)
        {
            return;
        }

        await client.WriteToStreamAsync(new RoomForwardEntryWriter
        {
            RoomId = targetPlayer.State.CurrentRoomId
        });
    }
}