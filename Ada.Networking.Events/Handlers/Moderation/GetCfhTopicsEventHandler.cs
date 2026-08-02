using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.GetCfhTopics)]
public class GetCfhTopicsEventHandler(IModerationTicketService ticketService)
    : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        await client.WriteToStreamAsync(new CfhTopicsInitWriter
        {
            Topics = await ticketService.GetTopicsAsync()
        });
    }
}
