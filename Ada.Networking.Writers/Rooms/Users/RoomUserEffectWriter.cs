using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserEffect)]
public class RoomUserEffectWriter : AbstractPacketWriter
{
    public required long UserId { get; init; }
    public required int EffectId { get; init; }
    public required int DelayMs { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteLong(UserId);
        writer.WriteInteger(EffectId);
        writer.WriteInteger(DelayMs);
    }
}