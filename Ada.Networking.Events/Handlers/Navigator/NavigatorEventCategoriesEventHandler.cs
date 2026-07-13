using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Navigator;

namespace Ada.Networking.Events.Handlers.Navigator;

[PacketId(EventHandlerId.NavigatorEventCategories)]
public class NavigatorEventCategoriesEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        await client.WriteToStreamAsync(new NavigatorEventCategoriesWriter
        {
            Categories = []
        });
    }
}