using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Groups.Forums;

[PacketId(EventHandlerId.UpdateForumReadMarker)]
public class UpdateForumReadMarkerEventHandler : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public int ThreadId { get; set; }
    public bool MarkRead { get; set; }

    public Task HandleAsync(INetworkClient client) => Task.CompletedTask;
}
