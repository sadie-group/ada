using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.UnseenResetCategory)]
public class UnseenResetCategoryEventHandler : INetworkPacketEventHandler
{
    public int Category { get; set; }

    public Task HandleAsync(INetworkClient client)
    {
        client.Player?.State.UnseenItems.ClearCategory(Category);
        return Task.CompletedTask;
    }
}
