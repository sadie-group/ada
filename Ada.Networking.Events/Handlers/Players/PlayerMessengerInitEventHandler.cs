using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db.Models.Constants;
using Ada.Networking.Writers.Players.Friendships;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerMessengerInit)]
public class PlayerMessengerInitEventHandler(
    ServerPlayerConstants playerConstants)
    : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new PlayerMessengerInitWriter
        {
            UserFriendLimit = playerConstants.MaxFriendships,
            NormalFriendLimit = playerConstants.MaxFriendships,
            ExtendedFriendLimit = playerConstants.MaxFriendships,
            FriendshipCategories = []
        });
    }
}