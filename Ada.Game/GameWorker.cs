using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using Ada.API;
using Ada.API.Interfaces.Game.Rooms;
using Ada.API.Interfaces.Game.Rooms.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ada.Game
{
    public class GameWorker(
        IRoomRepository roomRepository,
        IRoomWiredService wiredService,
        ILogger<GameWorker> logger) : IHostedService
    {
        private CancellationTokenSource? _cts;
        private Thread? _thread;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = new CancellationTokenSource();
            _thread = new Thread(() =>
            {
                try
                {
                    GameLoopAsync(_cts.Token).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    logger.LogCritical(e, "Game loop terminated");
                }
            })
            {
                IsBackground = true
            };

            _thread.Start();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        private const int _passIntervalMilliseconds = 100;
        private const int _passesPerTick = 5;

        private async Task GameLoopAsync(CancellationToken token)
        {
            var sw = new Stopwatch();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                CancellationToken = token
            };
            var pass = 0;

            while (!token.IsCancellationRequested)
            {
                sw.Restart();

                var fullTick = pass++ % _passesPerTick == 0;

                try
                {
                    await Parallel.ForEachAsync(
                        CollectActiveRooms(),
                        parallelOptions,
                        (room, _) => TickRoomSafelyAsync(room, fullTick));
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Game loop tick failed");
                }

                sw.Stop();

                var delay = _passIntervalMilliseconds - (int)sw.ElapsedMilliseconds;

                if (delay < 0)
                {
                    logger.LogWarning("Game loop tick is lagging by {ms}ms", -delay);
                }
                else if (delay > 0)
                {
                    try
                    {
                        await Task.Delay(delay, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private readonly List<IRoomLogic> _activeRooms = [];

        internal List<IRoomLogic> CollectActiveRooms()
        {
            _activeRooms.Clear();

            foreach (var room in roomRepository.GetAllRooms())
            {
                if (room.UserRepository.Count == 0)
                {
                    room.UserRepository.NoUsersSince ??= DateTime.UtcNow;
                    continue;
                }

                _activeRooms.Add(room);
            }

            return _activeRooms;
        }

        private async ValueTask TickRoomSafelyAsync(IRoomLogic room, bool fullTick)
        {
            try
            {
                await TickRoomAsync(room, fullTick);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Tick failed for room {RoomId}", room.Room.Id);
            }
        }

        private const int _stuckLockWarnAfterMilliseconds = 30_000;
        private const int _stuckLockRewarnEveryMilliseconds = 60_000;

        private readonly ConcurrentDictionary<int, long> _stuckLockLastWarned = new();

        private void WarnIfLockLooksStuck(IRoomLogic room)
        {
            var heldFor = room.LockHeldForMilliseconds;

            if (heldFor < _stuckLockWarnAfterMilliseconds)
            {
                if (heldFor == 0)
                {
                    _stuckLockLastWarned.TryRemove(room.Room.Id, out _);
                }

                return;
            }

            var now = Environment.TickCount64;

            if (_stuckLockLastWarned.TryGetValue(room.Room.Id, out var lastWarned) &&
                now - lastWarned < _stuckLockRewarnEveryMilliseconds)
            {
                return;
            }

            if (!_stuckLockLastWarned.TryUpdate(room.Room.Id, now, lastWarned) &&
                !_stuckLockLastWarned.TryAdd(room.Room.Id, now))
            {
                return;
            }

            logger.LogWarning(
                "Room {RoomId} has held its lock for {HeldForMs}ms; a packet handler is most likely stuck and this room is costing a parallel slot every tick",
                room.Room.Id,
                heldFor);
        }

        private async Task TickRoomAsync(IRoomLogic room, bool fullTick)
        {
            WarnIfLockLooksStuck(room);

            if (room.UserRepository.Count == 0)
            {
                room.UserRepository.NoUsersSince ??= DateTime.UtcNow;
                return;
            }

            List<INetworkObject>? toFlush = null;

            await room.RunLockedAsync(async () =>
            {
                if (fullTick)
                {
                    await room.BotRepository.RunPeriodicCheckAsync();
                    await room.PetRepository.RunPeriodicCheckAsync();
                    await room.UserRepository.RunPeriodicCheckAsync();
                    await wiredService.RunPeriodicTriggersForRoomAsync(room);
                }
                else
                {
                    await room.UserRepository.ProcessNewWalkRequestsAsync();
                }

                foreach (var user in room.UserRepository.GetAll())
                {
                    var obj = user.NetworkObject;

                    if (obj.WebSocket is not { State: WebSocketState.Open })
                    {
                        await room.UserRepository.TryRemoveAsync(user.Player.Player.Id, true);
                        continue;
                    }

                    (toFlush ??= []).Add(obj);
                }
            });

            if (toFlush == null)
            {
                return;
            }

            foreach (var obj in toFlush)
            {
                ObserveFlush(obj.FlushAsync(), room.Room.Id);
            }
        }

        private void ObserveFlush(Task flush, int roomId)
        {
            if (flush.IsCompletedSuccessfully)
            {
                return;
            }

            _ = flush.ContinueWith(
                (t, state) => logger.LogError(
                    t.Exception, "Flush failed for a client in room {RoomId}", state),
                roomId,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }
}
