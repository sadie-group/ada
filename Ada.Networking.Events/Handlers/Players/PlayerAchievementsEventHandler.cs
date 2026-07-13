using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerAchievements)]
public class PlayerAchievementsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new PlayerAchievementsWriter());
    }
}