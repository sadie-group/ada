using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;
using PlayerFriendshipStatus = Ada.Core.Enums.Game.Players.PlayerFriendshipStatus;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerProfile)]
public class PlayerProfileEventHandler(IPlayerRepository playerRepository)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int ProfileId { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        var profilePlayer = await playerRepository.GetPlayerByIdAsync(ProfileId);
        
        if (profilePlayer == null)
        {
            return;
        }
        
        var incomingAccepted = profilePlayer.IncomingFriendships.Count(x => x.Status == PlayerFriendshipStatus.Accepted);
        var outgoingAccepted = profilePlayer.OutgoingFriendships.Count(x => x.Status == PlayerFriendshipStatus.Accepted);
        var acceptedFriendCount = incomingAccepted + outgoingAccepted;
        
        var friendship = client.Player.TryGetFriendshipFor(ProfileId);

        var profileWriter = new PlayerProfileWriter
        {
            Player = profilePlayer,
            Online = profilePlayer.Data.IsOnline,
            FriendshipCount = acceptedFriendCount,
            FriendshipExists = friendship is { Status: PlayerFriendshipStatus.Accepted },
            FriendshipRequestExists = friendship is { Status: PlayerFriendshipStatus.Pending }
        };
        
        await client.WriteToStreamAsync(profileWriter);
    }
}