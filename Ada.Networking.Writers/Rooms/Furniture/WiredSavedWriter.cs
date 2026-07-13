using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.WiredSaved)] 
public class WiredSavedWriter : AbstractPacketWriter;