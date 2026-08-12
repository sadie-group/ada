using Ada.API;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Friendships;

[PacketId(ServerPacketId.PlayerRemoveFriends)]
public class PlayerRemoveFriendsWriter : AbstractPacketWriter
{
    public required int Unknown1 { get; init; }
    public required ICollection<long> PlayerIds { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Unknown1);
        writer.WriteInteger(PlayerIds.Count);

        foreach (var playerId in PlayerIds)
        {
            writer.WriteInteger(-1);
            writer.WriteInteger((int) playerId);
        }
    }
}