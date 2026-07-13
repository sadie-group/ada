using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Users;

[PacketId(ServerPacketId.RoomUserShout)]
public class RoomUserShoutWriter : RoomUserChatWriter;