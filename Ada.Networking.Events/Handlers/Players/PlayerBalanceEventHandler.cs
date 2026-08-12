using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Players;
using Ada.Core.Shared.Attributes;
using Ada.Networking.Writers.Players.Purse;

namespace Ada.Networking.Events.Handlers.Players;

[PacketId(EventHandlerId.PlayerBalance)]
public class PlayerBalanceEventHandler : INetworkPacketEventHandler
{
    public async Task HandleAsync(INetworkClient client)
    {
        if (client.Player == null)
        {
            return;
        }

        var playerData = client.Player.Player.Data;

        if (playerData == null)
        {
            return;
        }
        
        await client.WriteToStreamAsync(new PlayerCreditsBalanceWriter
        {
            Credits = playerData.CreditBalance
        });
        
        await client.WriteToStreamAsync(new PlayerActivityPointsBalanceWriter
        {
            Currencies = PlayerCurrencyMapper.FromBalances(
                playerData.PixelBalance,
                playerData.SeasonalBalance,
                playerData.GotwPoints)
        });
    }
}