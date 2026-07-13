using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerIgnoredUsers)]
public class PlayerIgnoredUsersEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var writer = new PlayerIgnoredUsersWriter
        {
            IgnoredUsernames = []
        };
        
        await client.WriteToStreamAsync(writer);
    }
}