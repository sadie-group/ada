using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.HotelView;

[PacketId(EventHandlerId.HotelViewPromotions)]
public class HotelViewPromotionsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
    }
}