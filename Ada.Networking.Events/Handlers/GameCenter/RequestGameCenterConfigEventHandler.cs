using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.GameCentre;

namespace Ada.Networking.Events.Handlers.GameCenter;

[PacketId(EventHandlerId.GameCenterRequestGames)]
public class RequestGameCenterConfigEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var writer = new PlayerGameAchievementsListWriter();
        await client.WriteToStreamAsync(writer);
    }
} 