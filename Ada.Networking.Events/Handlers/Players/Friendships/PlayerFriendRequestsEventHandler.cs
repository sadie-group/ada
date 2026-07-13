using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Events.Dtos;
using Ada.Networking.Writers.Players.Messenger;

namespace Ada.Networking.Events.Handlers.Players.Friendships;

[PacketId(EventHandlerId.PlayerFriendRequestsList)]
public class PlayerFriendRequestsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }
        
        var friendRequests = client
            .Player
            .Player
            .IncomingFriendships
            .Where(x => x.Status == PlayerFriendshipStatus.Pending)
            .ToList();

        var requests = new List<IPlayerFriendshipRequestData>();
        
        foreach (var data in friendRequests.Select(request => request.TargetPlayerId == client.Player.Player.Id ? 
                     request.OriginPlayer : 
                     request.TargetPlayer))
        {
            if (data?.AvatarData == null)
            {
                continue;
            }
            
            requests.Add(new PlayerFriendshipRequestData
            {
                Username = data.Username,
                FigureCode = data.AvatarData.FigureCode
            });
        }

        var requestsWriter = new PlayerFriendRequestsWriter
        {
            TotalRequests = requests.Count,
            Requests = requests
        };
        
        await client.WriteToStreamAsync(requestsWriter);
    }
}