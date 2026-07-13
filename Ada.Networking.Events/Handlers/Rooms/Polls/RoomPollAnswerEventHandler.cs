using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Rooms.Polls;

[PacketId(EventHandlerId.RoomPollAnswer)]
public class RoomPollAnswerEventHandler : INetworkPacketEventHandler
{
    public required int PollId { get; set; }
    public required int QuestionId { get; set; }
    public required List<string> Answers { get; set; }
    
    public Task HandleAsync(INetworkClient client)
    {
        throw new NotImplementedException();
    }
}