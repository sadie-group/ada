using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Catalog;

[PacketId(ServerPacketId.CatalogPurchaseFailed)]
public class CatalogPurchaseFailedWriter : AbstractPacketWriter
{
    public required int Error { get; init; }
}