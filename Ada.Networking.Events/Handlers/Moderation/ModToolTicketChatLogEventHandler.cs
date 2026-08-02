using Ada.API.Interfaces.Game.Moderation;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Moderation;

namespace Ada.Networking.Events.Handlers.Moderation;

[PacketId(EventHandlerId.ModToolsTicketChatLog)]
public class ModToolTicketChatLogEventHandler(
    IModerationTicketService ticketService,
    IModToolRepository modToolRepository,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IRunsOutsideRoomLock
{
    public int TicketId { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null || !player.HasPermission(PlayerPermissionName.Moderator))
        {
            return;
        }

        var ticket = ticketService.GetById(TicketId);

        if (ticket == null)
        {
            return;
        }

        var roomName = string.Empty;

        if (ticket.RoomId != null)
        {
            var room = roomRepository.TryGetRoomById(ticket.RoomId.Value);
            roomName = room?.Room.Name ?? string.Empty;
        }

        var reportedId = ticket.ReportedPlayerId ?? ticket.ReporterPlayerId;
        var (_, rooms) = await modToolRepository.GetUserChatlogAsync(reportedId, 1);
        var lines = rooms.SelectMany(x => x.Lines).ToList();

        await client.WriteToStreamAsync(new ModerationTicketChatLogWriter
        {
            TicketId = ticket.Id,
            ReporterPlayerId = ticket.ReporterPlayerId,
            ReportedPlayerId = reportedId,
            RoomId = ticket.RoomId ?? 0,
            RoomName = roomName,
            Lines = lines
        });
    }
}
