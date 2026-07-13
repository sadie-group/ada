using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Polls;

[PacketId(EventHandlerId.RoomPollReject)]
public class RoomPollRejectEventHandler : INetworkPacketEventHandler
{
    public int Unknown { get; init; }
    
    public Task HandleAsync(INetworkClient client)
    {
        throw new NotImplementedException();
    }
}