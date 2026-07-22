using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Rooms.Furniture;

[PacketId(ServerPacketId.WiredCondition)]
public class WiredConditionWriter : AbstractPacketWriter
{
    public required bool StuffTypeSelectionEnabled { get; init; }
    public required int MaxItemsSelected { get; init; }
    public required List<int> SelectedItemIds { get; init; }
    public required int AssetId { get; init; }
    public required int Id { get; init; }
    public required string Input { get; init; }
    public required List<int> IntParameters { get; init; }
    public required int StuffTypeSelectionCode { get; init; }
    public required int ConditionConfig { get; init; }
}
