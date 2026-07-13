using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ada.API.Interfaces.Game.Catalog;
using Ada.Game.Catalog.Purchase;

namespace Ada.Game.Catalog;

public class CatalogServiceCollection
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        serviceCollection.AddSingleton<ICatalogPageRepository, CatalogPagePageRepository>();
        serviceCollection.AddSingleton<ICatalogChargeService, CatalogChargeService>();
        serviceCollection.AddSingleton<ICatalogFurniturePurchaseService, CatalogFurniturePurchaseService>();
        serviceCollection.AddSingleton<ICatalogBotPurchaseService, CatalogBotPurchaseService>();
        serviceCollection.AddSingleton<ICatalogPurchaseConfirmationService, CatalogPurchaseConfirmationService>();
        serviceCollection.AddSingleton<ICatalogVipPurchaseService, CatalogVipPurchaseService>();
    }
}