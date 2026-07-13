using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Networking.Client;

namespace Ada.API.Interfaces.Game.Catalog;

public interface ICatalogBotPurchaseService
{
    Task ProcessAsync(INetworkClient client, CatalogItemDto item);
}