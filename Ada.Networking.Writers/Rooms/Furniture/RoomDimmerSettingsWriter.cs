using Ada.API;
using Ada.API.DTOs.Rooms;
using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.RoomDimmerSettings)]
public class RoomDimmerSettingsWriter : AbstractPacketWriter
{
    public required RoomDimmerSettingsDto DimmerSettings { get; init; }
    public required ICollection<RoomDimmerPresetDto> DimmerPresets { get; init; } = [];
    
    public override void OnSerialize(INetworkPacketWriter writer) 
    {
        writer.WriteInteger(DimmerPresets.Count);
        writer.WriteInteger(DimmerSettings.PresetId);

        foreach (var preset in DimmerPresets)
        {
            writer.WriteInteger(preset.PresetId);
            writer.WriteInteger(preset.BackgroundOnly ? 2 : 1);
            writer.WriteString(preset.Color);
            writer.WriteInteger(preset.Intensity);
        }
    }
}