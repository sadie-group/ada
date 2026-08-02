using Ada.API.DTOs.Catalog.FrontPage;
using Ada.API.DTOs.Catalog.Items;
using Ada.API.Interfaces.Networking;
using Ada.Core.Enums.Game.Catalog;
using Ada.Core.Enums.Game.Furniture;
using Ada.Core.Shared.Attributes;
using Ada.Core.Shared.Helpers;

namespace Ada.Networking.Writers.Catalog;

[PacketId(ServerPacketId.CatalogPage)]
public class CatalogPageWriter : AbstractPacketWriter
{
    public required int PageId { get; init; }
    public required string? CatalogMode { get; init; }
    public required string? PageLayout { get; init; }
    public required List<string?> Images { get; init; }
    public required List<string?> Texts { get; init; }
    public required List<CatalogItemDto> Items { get; init; }
    public required int Unknown { get; init; }
    public required bool AcceptSeasonCurrencyAsCredits { get; init; }
    public required IEnumerable<CatalogFrontPageItemDto> FrontPageItems { get; init; }

    public override void OnConfigureRules()
    {
        OverrideItems();
        OverrideFrontPageItems();
    }

    private void OverrideItems()
    {
        Override(nameof(Items), writer =>
        {
            writer.WriteInteger(Items.Count);

            foreach (var item in Items)
            {
                var itemName = item.Name ?? string.Empty;
                var metaData = item.MetaData ?? string.Empty;

                writer.WriteInteger(item.Id);
                writer.WriteString(itemName);
                writer.WriteBool(false);
                writer.WriteInteger(item.CostCredits);
                writer.WriteInteger(item.CostPoints);
                writer.WriteInteger(item.CostPointsType);
                writer.WriteBool(item.FurnitureItems.Any(x => x.CanGift));
                writer.WriteInteger(item.FurnitureItems.Count);

                foreach (var furnitureItem in item.FurnitureItems)
                {
                    writer.WriteString(EnumHelpers.GetEnumDescription(furnitureItem.Type));

                    if (furnitureItem.Type == FurnitureItemType.Badge)
                    {
                        writer.WriteString(furnitureItem.Name);
                    }
                    else
                    {
                        writer.WriteInteger(furnitureItem.AssetId);

                        if (itemName.Contains("_single_"))
                        {
                            writer.WriteString(itemName.Split("_")[2]);
                        }
                        else if (itemName.Contains("bot") && furnitureItem.Type == FurnitureItemType.Bot)
                        {
                            var look = metaData.Split(";").FirstOrDefault(x => x.StartsWith("figure:"));
                            writer.WriteString(!string.IsNullOrEmpty(look) ? look.Replace("figure:", "") : metaData);
                        }
                        else if (furnitureItem.Type == FurnitureItemType.Bot ||
                                 itemName.ToLower() == "poster" ||
                                 itemName.StartsWith("SONG "))
                        {
                            writer.WriteString(metaData);
                        }
                        else
                        {
                            writer.WriteString("");
                        }
                        
                        writer.WriteInteger(item.Amount);
                        writer.WriteBool(false);
                    }
                }

                writer.WriteInteger(item.RequiresClubMembership ? 1 : 0);
                writer.WriteBool(item.Amount == 1);
                writer.WriteBool(false);
                writer.WriteString($"{itemName}.png");
            }
        });
    }

    private void OverrideFrontPageItems()
    {
        Override(nameof(FrontPageItems), writer =>
        {
            if (PageLayout is not "frontpage4")
            {
                return;
            }
            
            writer.WriteInteger(FrontPageItems.Count());

            foreach (var item in FrontPageItems)
            {
                writer.WriteInteger(item.Id);
                writer.WriteString(item.Title ?? string.Empty);
                writer.WriteString(item.Image ?? string.Empty);
                writer.WriteInteger((int)item.TypeId);

                switch (item.TypeId)
                {
                    case CatalogFrontPageItemType.PageId:
                        writer.WriteInteger(item.CatalogPage?.Id ?? 0);
                        break;
                    case CatalogFrontPageItemType.PageName:
                        writer.WriteString(item.CatalogPage?.Name ?? string.Empty);
                        break;
                    case CatalogFrontPageItemType.ProductName:
                        writer.WriteString(item.ProductName ?? string.Empty);
                        break;
                    default:
                        throw new Exception($"Unknown catalog front page item type {(int)item.TypeId}");
                }

                writer.WriteInteger(-1);
            }
        });
    }
}