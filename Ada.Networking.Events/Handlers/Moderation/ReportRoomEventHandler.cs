using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ReportRoom)]
public class ReportRoomEventHandler(
    IModerationTicketService ticketService) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    private const int _maxMessageLength = 512;

    public int RoomId { get; set; }
    public string Message { get; set; } = "";
    public int CategoryId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var message = Message.Trim();

        if (message.Length > _maxMessageLength)
        {
            message = message[.._maxMessageLength];
        }

        await ticketService.CreateAsync(player.Player.Id, null, RoomId, CategoryId, message);
    }
}
