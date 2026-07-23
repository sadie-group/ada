using Ada.API.Interfaces.Game.Groups;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Groups;

namespace Ada.Networking.Events.Handlers.Groups;

[PacketId(EventHandlerId.RequestGuildInfo)]
public class RequestGuildInfoEventHandler(
    IGroupRepository groupRepository,
    IPlayerRepository playerRepository)
    : INetworkPacketEventHandler
{
    public int GuildId { get; set; }
    public bool NewWindow { get; set; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var group = await groupRepository.GetByIdAsync(GuildId);

        if (group == null)
        {
            return;
        }

        var membership = await groupRepository.GetMembershipAsync(group.Id, player.Player.Id);
        var membershipStatus = membership switch
        {
            null => 2,
            { IsPending: true } => 1,
            _ => 0
        };

        var isOwner = groupRepository.IsOwner(group, player.Player.Id);
        var isAdmin = await groupRepository.HasAdminRightsAsync(group, player.Player.Id);

        var pendingCount = 0;

        if (isAdmin)
        {
            (_, pendingCount) = await groupRepository.GetMembersAsync(group.Id, 0, "", 2, 1);
        }

        var ownerName = await playerRepository.GetPlayerUsernameByIdAsync(group.PlayerId) ?? "";
        var roomName = await groupRepository.GetRoomNameAsync(group.RoomId) ?? "";

        await client.WriteToStreamAsync(new GuildInfoWriter
        {
            GuildId = group.Id,
            State = (int) group.Type,
            Name = group.Name,
            Description = group.Description,
            Badge = group.Badge,
            RoomId = group.RoomId,
            RoomName = roomName,
            MembershipStatus = membershipStatus,
            MemberCount = await groupRepository.GetMemberCountAsync(group.Id),
            IsFavourite = false,
            DateCreated = DateTimeOffset.FromUnixTimeSeconds(group.CreatedAt).ToString("dd-MM-yyyy"),
            IsOwner = isOwner,
            IsAdmin = isAdmin,
            OwnerName = ownerName,
            NewWindow = NewWindow,
            OnlyAdminsCanDecorate = group.AdminOnlyDecoration,
            PendingRequestsCount = pendingCount,
            HasForum = group.HasForum
        });
    }
}
