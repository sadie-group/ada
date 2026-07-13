using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Unsorted;

[PacketId(EventHandlerId.GetBadgePointLimits)]
public class GetBadgePointLimitsEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
    }
}