using Ada.API;
using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers;

namespace Ada.Game.Players.Packets.Writers;

[PacketId(ServerPacketId.PlayerFriendsList)]
public class PlayerFriendsListWriter : AbstractPacketWriter
{
    public required int Pages { get; init; }
    public required int Index { get; init; }
    public required long PlayerId { get; init; }
    public required ICollection<PlayerFriendshipDto> Friends { get; init; }
    public required IPlayerRepository PlayerRepository { get; init; }
    public required ICollection<PlayerRelationshipDto> Relationships { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(Pages);
        writer.WriteInteger(Index);
        writer.WriteInteger(Friends.Count);

        foreach (var friend in Friends)
        {
            var friendData = friend.OriginPlayerId == PlayerId ? 
                friend.TargetPlayer : 
                friend.OriginPlayer;

            if (friendData == null)
            {
                continue;
            }

            var onlineFriend = PlayerRepository.GetPlayerLogicById(friendData.Id);
            var isOnline = onlineFriend != null;
            var inRoom = isOnline && onlineFriend != null && onlineFriend.State.CurrentRoomId != 0;
            
            var relationshipType = Relationships
               .FirstOrDefault(x => x.TargetPlayerId == friendData.Id)
               ?.TypeId ?? (int) PlayerRelationshipType.None;

            writer.WriteInteger((int) friendData.Id);
            writer.WriteString(friendData.Username);
            writer.WriteInteger(friendData.AvatarData?.Gender == PlayerAvatarGender.Male ? 0 : 1);
            writer.WriteBool(isOnline);
            writer.WriteBool(inRoom);
            writer.WriteString(friendData.AvatarData?.FigureCode ?? string.Empty);
            writer.WriteInteger(0); // category ID
            writer.WriteString(friendData.AvatarData?.Motto ?? string.Empty);
            writer.WriteString(friendData.Username); // real name
            writer.WriteString(""); // last access?
            writer.WriteBool(false);
            writer.WriteBool(false); // VIP
            writer.WriteBool(false); // pocket
            writer.WriteShort((short) relationshipType);
        }
    }
}
