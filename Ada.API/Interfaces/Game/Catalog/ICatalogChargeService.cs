using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Game.Catalog;

public interface ICatalogChargeService
{
    bool HasRequiredMembership(INetworkClient client, CatalogItemDto item);
    Task<bool> TryChargeAsync(INetworkClient client, CatalogItemDto item, int amount);
}