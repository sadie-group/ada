using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Camera;

[PacketId(ServerPacketId.CameraPrice)]
public class CameraPriceWriter : AbstractPacketWriter
{
    public required int CostCredits { get; init; }
    public required int CostPoints { get; init; }
    public required int PointsType { get; init; }
}