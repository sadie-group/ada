using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Game.Catalog;

public interface ICatalogVipPurchaseService
{
    Task ProcessAsync(INetworkClient client, int itemId);
}