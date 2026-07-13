using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Catalog;

[PacketId(ServerPacketId.CatalogGiftConfig)]
public class CatalogGiftWrappingConfigWriter : AbstractPacketWriter
{
    public required bool Enabled { get; init; }
    public required int Price { get; init; }
    public required List<int> GiftWrappers { get; init; }
    public required List<int> BoxTypes { get; init; }
    public required List<int> RibbonTypes { get; init; }
    public required List<int> GiftFurniture { get; init; }
}