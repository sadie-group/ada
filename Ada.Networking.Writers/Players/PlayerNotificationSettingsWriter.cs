using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.NotificationSettings)]
public class PlayerNotificationSettingsWriter : AbstractPacketWriter
{
    public required bool ShowNotifications { get; init; }
}