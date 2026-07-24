using System.Diagnostics;
using System.Net.WebSockets;
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
        private readonly ParallelOptions _parallelOptions = new()
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

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

        private async Task GameLoopAsync(CancellationToken token)
        {
            var sw = new Stopwatch();

            while (!token.IsCancellationRequested)
            {
                sw.Restart();

                try
                {
                    await Parallel.ForEachAsync(
                        roomRepository.GetAllRooms(),
                        new ParallelOptions
                        {
                            MaxDegreeOfParallelism = _parallelOptions.MaxDegreeOfParallelism,
                            CancellationToken = token
                        },
                        async (room, _) => await TickRoomAsync(room));
                }
                catch (OperationCanceledException)
                {
                    break;
                            await room.BotRepository.RunPeriodicCheckAsync();
                            await room.PetRepository.RunPeriodicCheckAsync();
                            await room.UserRepository.RunPeriodicCheckAsync();
                            await wiredService.RunPeriodicTriggersForRoomAsync(room);

                            foreach (var user in room.UserRepository.GetAll())
                            {
                                var obj = user.NetworkObject;

                                if (obj.Outbox.Count <= 0)
                                {
                                    continue;
                                }
                                
                                var socket = obj.WebSocket;

                                if (socket is { State: WebSocketState.Open })
                                {
                                    var payload = obj.Outbox
                                        .SelectMany(x => x.GetAllBytes())
                                        .ToArray();

                                    try
                                    {
                                        await socket.SendAsync(
                                            payload,
                                            WebSocketMessageType.Binary,
                                            true,
                                            CancellationToken.None
                                        );
                                    }
                                    catch
                                    {
                                        await room.UserRepository.TryRemoveAsync(
                                            user.Player.Player.Id,
                                            true
                                        );
                                    }
                                }
                                else
                                {
                                    await room.UserRepository.TryRemoveAsync(
                                        user.Player.Player.Id,
                                        true
                                    );
                                }

                                obj.Outbox.Clear();
                            }
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }, token);

                    roomTasks.Add(roomTask);
                }

                sw.Stop();

                var delay = 500 - (int)sw.ElapsedMilliseconds;

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

        private async Task TickRoomAsync(IRoomLogic room)
        {
            await room.BotRepository.RunPeriodicCheckAsync();
            await room.UserRepository.RunPeriodicCheckAsync();
            await wiredService.RunPeriodicTriggersForRoomAsync(room);

            foreach (var user in room.UserRepository.GetAll())
            {
                var obj = user.NetworkObject;

                if (obj.WebSocket is not { State: WebSocketState.Open })
                {
                    await room.UserRepository.TryRemoveAsync(user.Player.Player.Id, true);
                    continue;
                }

                try
                {
                    await obj.FlushAsync();
                }
                catch
                {
                    await room.UserRepository.TryRemoveAsync(user.Player.Player.Id, true);
                }
            }
        }
    }
}
