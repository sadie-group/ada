using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Polls;

[PacketId(EventHandlerId.RoomPollVoteCounter)]
public class RoomPollVoteCounterEventHandler : INetworkPacketEventHandler
{
    public int Counter { get; init; }

    public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
}
