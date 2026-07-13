using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserChat)]
public class RoomUserChatWriter : RoomUserWhisperWriter;