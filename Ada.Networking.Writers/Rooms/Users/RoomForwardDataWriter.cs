using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Rooms;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomForwardData)]
public class RoomForwardDataWriter : AbstractPacketWriter
{
    public required RoomDto Room { get; init; }
    public required bool RoomForward { get; init; }
    public required bool EnterRoom { get; init; }
    public required bool IsOwner { get; init; }
    public required int UsersNow { get; init; }
    public required string OwnerUsername { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        var settings = Room.Settings;
        var chatSettings = Room.ChatSettings;

        if (settings == null || chatSettings == null)
        {
            throw new InvalidOperationException(
                $"Room {Room.Id} has no settings or chat settings loaded and cannot be serialized");
        }

        writer.WriteBool(EnterRoom);
        writer.WriteLong(Room.Id);
        writer.WriteString(Room.Name);
        writer.WriteLong(Room.OwnerId);
        writer.WriteString(OwnerUsername);
        writer.WriteInteger((int) settings.AccessType);
        writer.WriteInteger(UsersNow);
        writer.WriteInteger(Room.MaxUsersAllowed);
        writer.WriteString(Room.Description);
        writer.WriteInteger((int) settings.TradeOption);
        writer.WriteInteger(Room.PlayerLikes.Count);
        writer.WriteInteger(1);
        writer.WriteInteger(0);
        writer.WriteInteger(Room.Tags.Count);

        foreach (var tag in Room.Tags)
        {
            writer.WriteString(tag.Name);
        }

        writer.WriteInteger((int) RoomBitmask.ShowOwner);
        writer.WriteBool(RoomForward);
        writer.WriteBool(false);
        writer.WriteBool(false);
        writer.WriteBool(Room.IsMuted);
        writer.WriteInteger(settings.WhoCanMute);
        writer.WriteInteger(settings.WhoCanKick);
        writer.WriteInteger(settings.WhoCanBan);
        writer.WriteBool(IsOwner);
        writer.WriteInteger(chatSettings.ChatType);
        writer.WriteInteger(chatSettings.ChatWeight);
        writer.WriteInteger(chatSettings.ChatSpeed);
        writer.WriteInteger(chatSettings.ChatDistance);
        writer.WriteInteger(chatSettings.ChatProtection);
    }
}
