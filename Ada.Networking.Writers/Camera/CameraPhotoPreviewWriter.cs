using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Camera;

[PacketId(ServerPacketId.CameraPhotoPreview)]
public class CameraPhotoPreviewWriter : AbstractPacketWriter
{
    public required string Url { get; init; }
}
