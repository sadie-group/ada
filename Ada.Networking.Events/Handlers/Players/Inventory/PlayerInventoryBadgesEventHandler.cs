using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Inventory;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.PlayerInventoryBadges)]
public class PlayerInventoryBadgesEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        var badges = client.Player.Player.Badges
            .ToDictionary(x => x.Id, x => x.Badge?.Code ?? "");
        
        var equippedBadges = client.Player.Player.Badges
            .Where(x => x.Slot is > 0 and <= 5)
            .ToDictionary(x => x.Id, x => x.Badge?.Code ?? "");
        
        await client.WriteToStreamAsync(new PlayerInventoryBadgesWriter
        {
            Badges = badges,
            EquippedBadges = equippedBadges
        });
    }
}