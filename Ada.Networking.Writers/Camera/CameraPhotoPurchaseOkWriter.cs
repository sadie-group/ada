using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Camera;

[PacketId(ServerPacketId.CameraPhotoPurchaseOk)]
public class CameraPhotoPurchaseOkWriter : AbstractPacketWriter;
