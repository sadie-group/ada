using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Players.Inventory;

[PacketId(EventHandlerId.UnseenResetItems)]
public class UnseenResetItemsEventHandler : INetworkPacketEventHandler
{
    public int Category { get; set; }
    public List<int> ItemIds { get; set; } = [];

    public Task HandleAsync(INetworkClient client)
    {
        client.Player?.State.UnseenItems.Remove(Category, ItemIds);
        return Task.CompletedTask;
    }
}
