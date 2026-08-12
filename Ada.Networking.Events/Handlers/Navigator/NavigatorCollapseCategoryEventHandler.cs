using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorCollapseCategory)]
public class NavigatorCollapseCategoryEventHandler : INetworkPacketEventHandler
{
    public string? Category { get; set; }

    public Task HandleAsync(INetworkClient client)
    {
        if (client.Player != null && !string.IsNullOrWhiteSpace(Category))
        {
            client.Player.State.Navigator.Collapse(Category.Trim());
        }

        return Task.CompletedTask;
    }
}
