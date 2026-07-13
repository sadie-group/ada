using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Groups;

namespace Ada.Networking.Events.Handlers.Players.Groups;

[PacketId(EventHandlerId.GetPlayerGroupBadgeParts)]
public class GetPlayerGroupBadgePartsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var badgeParts = new PlayerGroupBadgePartsWriter
        {
            Bases = [],
            Symbols = [],
            PartColors = [],
            PrimaryColors = [],
            SecondaryColors = []
        }; // TODO: Populate
        
        await client.WriteToStreamAsync(badgeParts);
    }
}