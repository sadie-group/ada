using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.API.Interfaces.Networking;

namespace Ada.Networking.Events.Handlers.Guides;

internal static class GuideSessionBroadcast
{
    public static IPlayerLogic? PartnerOf(IGuideSession session, long playerId, IPlayerRepository playerRepository)
    {
        var partnerId = session.RequesterId == playerId
            ? session.HelperId
            : session.RequesterId;

        return partnerId == null ? null : playerRepository.GetPlayerLogicById(partnerId.Value);
    }

    public static async Task ToBothAsync(
        IGuideSession session,
        IPlayerRepository playerRepository,
        AbstractPacketWriter writer)
    {
        var requester = playerRepository.GetPlayerLogicById(session.RequesterId);

        if (requester?.NetworkObject != null)
        {
            await requester.NetworkObject.WriteToStreamAsync(writer);
        }

        if (session.HelperId is not { } helperId)
        {
            return;
        }

        var helper = playerRepository.GetPlayerLogicById(helperId);

        if (helper?.NetworkObject != null)
        {
            await helper.NetworkObject.WriteToStreamAsync(writer);
        }
    }
}
