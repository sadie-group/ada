using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players;

[PacketId(ServerPacketId.PlayerBadges)]
public class PlayerWearingBadgesWriter : AbstractPacketWriter
{
    public required int PlayerId { get; init; }
    public required ICollection<PlayerBadgeDto> Badges { get; init; }

    public override void OnConfigureRules()
    {
        Override(GetType().GetProperty(nameof(Badges))!, writer =>
        {
            writer.WriteInteger(Badges.Count);

            foreach (var item in Badges)
            {
                writer.WriteInteger(item.Slot);
                writer.WriteString(item.Badge.Code);
            }
        });
    }
}