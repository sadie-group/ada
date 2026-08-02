using Ada.API;
using Ada.API.Interfaces.Game.Players.Friendships;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Players;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Players.Friendships;

[PacketId(ServerPacketId.PlayerUpdateFriend)]
public class PlayerUpdateFriendWriter : AbstractPacketWriter
{
    public required List<IPlayerFriendshipUpdate> Updates { get; init; }

    public override void OnSerialize(INetworkPacketWriter writer)
    {
        writer.WriteInteger(0);
        writer.WriteInteger(Updates.Count);

        foreach (var update in Updates)
        {
            writer.WriteInteger(update.Type);

            if (update.Type == -1)
            {
                writer.WriteLong(update.Friend!.Id);
            }
            else
            {
                var friend = update.Friend;

                writer.WriteLong(friend?.Id ?? 0);
                writer.WriteString(friend?.Username ?? string.Empty);
                writer.WriteInteger(friend?.Gender == PlayerAvatarGender.Male ? 0 : 1);
                writer.WriteBool(update.FriendOnline);
                writer.WriteBool(update.FriendInRoom);
                writer.WriteString(friend?.FigureCode ?? string.Empty);
                writer.WriteInteger(0);
                writer.WriteString(friend?.Motto ?? string.Empty);
                writer.WriteString("");
                writer.WriteString("");
                writer.WriteBool(false);
                writer.WriteBool(false);
                writer.WriteBool(false);
                writer.WriteInteger((int) update.Relation);
            }
        }
    }
}