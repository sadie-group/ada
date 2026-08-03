using Ada.API.DTOs.Players;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Players;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players.Purse;
using Ada.Networking.Writers.Rooms.Furniture;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Rooms.Furniture;

[PacketId(EventHandlerId.RedeemItem)]
public class RoomRedeemItemEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory) : INetworkPacketEventHandler
{
    public required int ItemId { get; init; }

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;
        var room = client.RoomUser!.Room;

        if (player?.NetworkObject == null ||
            client.RoomUser == null ||
            room.Room.OwnerId != player.Player.Id)
        {
            return;
        }

        var data = player.Player.Data;

        if (data == null)
        {
            return;
        }

        var roomFurnitureItem = room
            .Room.FurnitureItems
            .FirstOrDefault(x => x.PlayerFurnitureItemId == ItemId);

        if (roomFurnitureItem == null)
        {
            return;
        }

        if (roomFurnitureItem.PlayerFurnitureItem.PlayerId != player.Player.Id)
        {
            return;
        }

        var assetName = roomFurnitureItem
            .PlayerFurnitureItem
            .FurnitureItem
            .AssetName;

        if (!TryGetRedemptionValue(assetName, out var currency, out var value))
        {
            return;
        }

        await room.BroadcastDataAsync(new RoomFloorFurnitureItemRemovedWriter
        {
            Id = roomFurnitureItem.PlayerFurnitureItemId.ToString(),
            Expired = false,
            OwnerId = 0,
            Delay = 0
        });

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        room.Room.FurnitureItems.Remove(roomFurnitureItem);

        var inventoryItem = player.Player.FurnitureItems
            .FirstOrDefault(x => x.Id == roomFurnitureItem.PlayerFurnitureItemId);

        if (inventoryItem != null)
        {
            player.Player.FurnitureItems.Remove(inventoryItem);
        }

        await dbContext.RoomFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.Id)
            .ExecuteDeleteAsync();

        await dbContext.PlayerFurnitureItems
            .Where(x => x.Id == roomFurnitureItem.PlayerFurnitureItem.Id)
            .ExecuteDeleteAsync();

        switch (currency)
        {
            case RedemptionCurrency.Credits:
                data.CreditBalance += value;

                await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
                {
                    Credits = data.CreditBalance
                });
                break;

            case RedemptionCurrency.Pixels:
                data.PixelBalance += value;
                await WriteActivityPointsAsync(client, data);
                break;

            case RedemptionCurrency.Seasonal:
                data.SeasonalBalance += value;
                await WriteActivityPointsAsync(client, data);
                break;

            case RedemptionCurrency.Gotw:
                data.GotwPoints += value;
                await WriteActivityPointsAsync(client, data);
                break;
        }

        await dbContext.PlayerData
            .Where(x => x.PlayerId == player.Player.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.PixelBalance, data.PixelBalance)
                .SetProperty(x => x.CreditBalance, data.CreditBalance)
                .SetProperty(x => x.SeasonalBalance, data.SeasonalBalance)
                .SetProperty(x => x.GotwPoints, data.GotwPoints));
    }

    private enum RedemptionCurrency
    {
        None,
        Credits,
        Pixels,
        Seasonal,
        Gotw
    }

    private static bool TryGetRedemptionValue(string assetName, out RedemptionCurrency currency, out int value)
    {
        currency = RedemptionCurrency.None;
        value = 0;

        var parts = assetName.Split('_');

        if (parts.Length < 2)
        {
            return false;
        }

        switch (parts[0])
        {
            case "CF":
            case "CFC":
                if (parts[1] == "diamond")
                {
                    currency = RedemptionCurrency.Seasonal;
                    return parts.Length >= 3 && TryParseValue(parts[2], out value);
                }

                currency = RedemptionCurrency.Credits;
                return TryParseValue(parts[1], out value);

            case "PF":
                currency = RedemptionCurrency.Pixels;
                return TryParseValue(parts[1], out value);

            case "DF":
                if (parts.Length < 3 ||
                    !int.TryParse(parts[1], out var pointsType) ||
                    !TryParseValue(parts[2], out value))
                {
                    return false;
                }

                currency = pointsType switch
                {
                    5 => RedemptionCurrency.Seasonal,
                    103 => RedemptionCurrency.Gotw,
                    _ => RedemptionCurrency.None
                };

                return currency != RedemptionCurrency.None;

            default:
                return false;
        }
    }

    private static bool TryParseValue(string raw, out int value)
        => int.TryParse(raw, out value) && value >= 0;

    private static Task WriteActivityPointsAsync(INetworkClient client, PlayerDataDto data)
        => client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
        {
            Currencies = PlayerCurrencyMapper.FromBalances(
                data.PixelBalance,
                data.SeasonalBalance,
                data.GotwPoints)
        });
}
