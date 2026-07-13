using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Subscriptions;

namespace Ada.Networking.Events.Handlers.Players.Club;

[PacketId(EventHandlerId.PlayerSubscription)]
public class PlayerSubscriptionEventHandler(IPlayerHelperService playerHelperService) : INetworkPacketEventHandler
{
    public string? Name { get; set; }
    
    public async Task HandleAsync(INetworkClient client)
    {
        if (string.IsNullOrEmpty(Name) || client.Player == null)
        {
            return;
        }

        var writer = playerHelperService.GetSubscriptionWriterAsync(client.Player, Name);

        if (writer == null)
        {
            return;
        }
        
        await client.WriteToStreamAsync((PlayerSubscriptionWriter) writer);
        client.Player.State.LastSubscriptionModification = DateTime.Now;
    }
}