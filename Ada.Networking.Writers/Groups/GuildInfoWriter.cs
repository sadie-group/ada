using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Groups;

[PacketId(ServerPacketId.GuildInfo)]
public class GuildInfoWriter : AbstractPacketWriter
{
    public required int GuildId { get; init; }
    public required int State { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string Badge { get; init; }
    public required int RoomId { get; init; }
    public required string RoomName { get; init; }
    public required int MembershipStatus { get; init; }
    public required int MemberCount { get; init; }
    public required bool IsFavourite { get; init; }
    public required string DateCreated { get; init; }
    public required bool IsOwner { get; init; }
    public required bool IsAdmin { get; init; }
    public required string OwnerName { get; init; }
    public required bool NewWindow { get; init; }
    public required bool OnlyAdminsCanDecorate { get; init; }
    public required int PendingRequestsCount { get; init; }
    public required bool HasForum { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(GuildId);
        writer.WriteBool(true);
        writer.WriteInteger(State);
        writer.WriteString(Name);
        writer.WriteString(Description);
        writer.WriteString(Badge);
        writer.WriteInteger(RoomId);
        writer.WriteString(RoomName);
        writer.WriteInteger(MembershipStatus);
        writer.WriteInteger(MemberCount);
        writer.WriteBool(IsFavourite);
        writer.WriteString(DateCreated);
        writer.WriteBool(IsOwner);
        writer.WriteBool(IsAdmin);
        writer.WriteString(OwnerName);
        writer.WriteBool(NewWindow);
        writer.WriteBool(OnlyAdminsCanDecorate);
        writer.WriteInteger(PendingRequestsCount);
        writer.WriteBool(HasForum);
    }
}
