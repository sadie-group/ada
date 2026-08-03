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
                GameLoopAsync(_cts.Token).GetAwaiter().GetResult();
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

        private const int PassIntervalMilliseconds = 100;
        private const int PassesPerTick = 5;

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

                var fullTick = pass++ % PassesPerTick == 0;

                try
                {
                    await Parallel.ForEachAsync(
                        roomRepository.GetAllRooms(),
                        parallelOptions,
                        async (room, _) => await TickRoomAsync(room, fullTick));
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                sw.Stop();

                var delay = PassIntervalMilliseconds - (int)sw.ElapsedMilliseconds;

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

        private async Task TickRoomAsync(IRoomLogic room, bool fullTick)
        {
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
                _ = obj.FlushAsync();
            }
        }
    }
}
