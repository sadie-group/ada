using Ada.API.Interfaces.Networking;
using Ada.Core.Shared.Attributes;

namespace Ada.Networking.Writers.Catalog;

[PacketId(ServerPacketId.CatalogPurchaseUnavailable)]
public class CatalogPurchaseUnavailableWriter : AbstractPacketWriter
{
    public int Code { get; init; }
}