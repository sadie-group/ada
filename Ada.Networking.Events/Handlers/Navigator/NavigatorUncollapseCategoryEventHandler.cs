using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorUncollapseCategory)]
public class NavigatorUncollapseCategoryEventHandler : INetworkPacketEventHandler
{
    public string? Category { get; set; }

    public Task HandleAsync(INetworkClient client)
    {
        if (client.Player != null && !string.IsNullOrWhiteSpace(Category))
        {
            client.Player.State.Navigator.Expand(Category.Trim());
        }

        return Task.CompletedTask;
    }
}
