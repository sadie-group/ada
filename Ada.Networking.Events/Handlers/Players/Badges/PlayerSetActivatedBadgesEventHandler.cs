using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.Core.Shared.Attributes;
using Ada.Db;
using Ada.Networking.Writers.Players;
using Microsoft.EntityFrameworkCore;

namespace Ada.Networking.Events.Handlers.Players.Badges;

[PacketId(EventHandlerId.PlayerSetActivatedBadges)]
public class PlayerSetActivatedBadgesEventHandler(
    IDbContextFactory<AdaDbContext> dbContextFactory,
    IRoomRepository roomRepository) : INetworkPacketEventHandler, IDefersPersistence
{
    public required List<string> BadgeCodes { get; init; }

    private const int _maxWornBadges = 5;

    public async Task HandleAsync(INetworkClient client)
    {
        var player = client.Player;

        if (player == null)
        {
            return;
        }

        var requested = BadgeCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(_maxWornBadges)
            .ToList();

        var owned = player.Player.Badges;
        var slotAssignments = new Dictionary<int, int>();
        var slot = 1;

        foreach (var badge in owned)
        {
            var index = requested.IndexOf(badge.Badge?.Code ?? string.Empty);

            badge.Slot = index >= 0 ? slot++ : 0;

            slotAssignments[badge.Id] = badge.Slot;
        }

        _persist = async () =>
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();

            foreach (var (badgeId, assignedSlot) in slotAssignments)
            {
                await dbContext.PlayerBadges
                    .Where(x => x.Id == badgeId)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.Slot, assignedSlot));
            }
        };

        var worn = owned.Where(x => x.Slot > 0).ToList();

        var writer = new PlayerWearingBadgesWriter
        {
            PlayerId = (int) player.Player.Id,
            Badges = worn
        };

        var room = roomRepository.TryGetRoomById(player.State.CurrentRoomId);

        if (room != null)
        {
            await room.BroadcastDataAsync(writer);
            return;
        }

        await client.WriteToStreamAsync(writer);
    }

    private Func<Task>? _persist;

    public Task PersistAsync() => _persist?.Invoke() ?? Task.CompletedTask;
}
