using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Guides;
using Ada.API.Interfaces.Game.Players;
using Ada.Core.Enums.Game.Players;

namespace Ada.Game.Guides;

public sealed class GuideSession : IGuideSession
{
    public required long RequesterId { get; init; }
    public long? HelperId { get; set; }
    public required string HelpRequest { get; init; }
    public required int RequestType { get; init; }
    public GuideSessionState State { get; set; } = GuideSessionState.Pending;
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public HashSet<long> DeclinedBy { get; } = [];
}

public class GuideSessionService : IGuideSessionService
{
    private const int _maxRecentWaits = 20;
    private const int _defaultWaitSeconds = 60;

    private readonly ConcurrentDictionary<long, bool> _onDuty = new();
    private readonly ConcurrentDictionary<long, bool> _guardians = new();
    private readonly ConcurrentDictionary<long, GuideSession> _byRequester = new();
    private readonly ConcurrentDictionary<long, GuideSession> _byHelper = new();
    private readonly ConcurrentQueue<int> _recentWaits = new();

    private readonly object _assignmentLock = new();

    public int GuidesOnDuty => _onDuty.Count;

    public int GuardiansOnDuty => _guardians.Count;

    public int AverageWaitSeconds
    {
        get
        {
            var samples = _recentWaits.ToArray();

            return samples.Length == 0
                ? _defaultWaitSeconds
                : (int) samples.Average();
        }
    }

    public bool SetOnDuty(long playerId, bool onDuty, bool asGuardian)
    {
        var target = asGuardian ? _guardians : _onDuty;

        if (onDuty)
        {
            return target.TryAdd(playerId, true);
        }

        return target.TryRemove(playerId, out _);
    }

    public bool IsOnDuty(long playerId) => _onDuty.ContainsKey(playerId);

    public IGuideSession? GetSessionForPlayer(long playerId)
    {
        if (_byRequester.TryGetValue(playerId, out var asRequester))
        {
            return asRequester;
        }

        return _byHelper.TryGetValue(playerId, out var asHelper) ? asHelper : null;
    }

    public IGuideSession? GetPendingSessionForHelper(long helperId)
        => _byHelper.TryGetValue(helperId, out var session) && session.State == GuideSessionState.Pending
            ? session
            : null;

    public IGuideSession CreateSession(long requesterId, int requestType, string helpRequest)
    {
        var session = new GuideSession
        {
            RequesterId = requesterId,
            RequestType = requestType,
            HelpRequest = helpRequest,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _byRequester[requesterId] = session;

        return session;
    }

    public long? TryAssignHelper(IGuideSession session, IEnumerable<IPlayerLogic> candidates)
    {
        if (session is not GuideSession concrete)
        {
            return null;
        }

        lock (_assignmentLock)
        {
            if (concrete.HelperId != null)
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                var candidateId = candidate.Player.Id;

                if (candidateId == concrete.RequesterId ||
                    concrete.DeclinedBy.Contains(candidateId) ||
                    !_onDuty.ContainsKey(candidateId) ||
                    _byHelper.ContainsKey(candidateId) ||
                    _byRequester.ContainsKey(candidateId) ||
                    !candidate.HasPermission(PlayerPermissionName.GuideUseTool))
                {
                    continue;
                }

                concrete.HelperId = candidateId;
                _byHelper[candidateId] = concrete;

                return candidateId;
            }

            return null;
        }
    }

    public bool Accept(IGuideSession session, long helperId)
    {
        if (session is not GuideSession concrete)
        {
            return false;
        }

        lock (_assignmentLock)
        {
            if (concrete.HelperId != helperId || concrete.State != GuideSessionState.Pending)
            {
                return false;
            }

            concrete.State = GuideSessionState.Active;
            concrete.AcceptedAt = DateTimeOffset.UtcNow;

            RecordWait((int) (concrete.AcceptedAt.Value - concrete.CreatedAt).TotalSeconds);

            return true;
        }
    }

    public void Decline(IGuideSession session)
    {
        if (session is not GuideSession concrete)
        {
            return;
        }

        lock (_assignmentLock)
        {
            if (concrete.HelperId is { } helperId)
            {
                concrete.DeclinedBy.Add(helperId);
                _byHelper.TryRemove(helperId, out _);
            }

            concrete.HelperId = null;
            concrete.State = GuideSessionState.Pending;
        }
    }

    public bool End(IGuideSession session)
    {
        if (session is not GuideSession concrete)
        {
            return false;
        }

        lock (_assignmentLock)
        {
            var removed = _byRequester.TryRemove(concrete.RequesterId, out _);

            if (concrete.HelperId is { } helperId)
            {
                removed |= _byHelper.TryRemove(helperId, out _);
            }

            return removed;
        }
    }

    public void Forget(long playerId)
    {
        _onDuty.TryRemove(playerId, out _);
        _guardians.TryRemove(playerId, out _);

        if (GetSessionForPlayer(playerId) is { } session)
        {
            End(session);
        }
    }

    private void RecordWait(int seconds)
    {
        _recentWaits.Enqueue(Math.Max(seconds, 0));

        while (_recentWaits.Count > _maxRecentWaits)
        {
            _recentWaits.TryDequeue(out _);
        }
    }
}
