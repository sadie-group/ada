using System.Collections.Concurrent;
using Ada.API.Interfaces.Game.Rooms.Services;

namespace Ada.Game.Rooms.Services;

public class RoomFloodProtectionService : IRoomFloodProtectionService
{
    public const int BaseMuteSeconds = 30;
    private const int _baseThreshold = 3;

    private static readonly TimeSpan _escalationDecayAfter = TimeSpan.FromMinutes(10);

    private const int _maxEscalationSteps = 10;

    private class PlayerFloodState
    {
        public double Counter;
        public DateTimeOffset LastMessageAt = DateTimeOffset.MinValue;
        public DateTimeOffset MutedUntil = DateTimeOffset.MinValue;
        public DateTimeOffset LastMutedAt = DateTimeOffset.MinValue;
        public int MutedCount;
    }

    private readonly ConcurrentDictionary<long, PlayerFloodState> _states = new();

    public bool IsMuted(long playerId, out int remainingSeconds)
    {
        remainingSeconds = 0;

        if (!_states.TryGetValue(playerId, out var state))
        {
            return false;
        }

        lock (state)
        {
            var remaining = state.MutedUntil - DateTimeOffset.Now;

            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return true;
        }
    }

    public int? RegisterMessage(long playerId, int chatProtection, bool bypass)
    {
        if (bypass)
        {
            return null;
        }

        var state = _states.GetOrAdd(playerId, _ => new PlayerFloodState());

        lock (state)
        {
            var now = DateTimeOffset.Now;

            if (state.LastMessageAt > DateTimeOffset.MinValue)
            {
                state.Counter = Math.Max(0, state.Counter - (now - state.LastMessageAt).TotalSeconds);
            }

            state.Counter++;
            state.LastMessageAt = now;

            var threshold = _baseThreshold + Math.Clamp(chatProtection, 0, 2);

            if (state.Counter <= threshold)
            {
                return null;
            }

            if (state.LastMutedAt > DateTimeOffset.MinValue &&
                now - state.LastMutedAt > _escalationDecayAfter)
            {
                var stepsForgiven = (int)((now - state.LastMutedAt).Ticks / _escalationDecayAfter.Ticks);

                state.MutedCount = Math.Max(0, state.MutedCount - stepsForgiven);
            }

            state.MutedCount = Math.Min(state.MutedCount + 1, _maxEscalationSteps);
            state.LastMutedAt = now;
            state.Counter = 0;

            var muteSeconds = BaseMuteSeconds +
                              BaseMuteSeconds * (int)Math.Ceiling(Math.Pow(state.MutedCount, 2));

            state.MutedUntil = now.AddSeconds(muteSeconds);
            return muteSeconds;
        }
    }

    public void MuteFor(long playerId, int seconds)
    {
        var state = _states.GetOrAdd(playerId, _ => new PlayerFloodState());

        lock (state)
        {
            state.MutedUntil = DateTimeOffset.Now.AddSeconds(seconds);
        }
    }

    public void Clear(long playerId)
    {
        _states.TryRemove(playerId, out _);
    }
}
