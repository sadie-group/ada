using System.Collections.Concurrent;
using System.Net;
using Ada.API.Interfaces.Networking.Client;
using Ada.Networking.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ada.Networking;

public class NetworkListener(
    IOptions<NetworkOptions> options,
    ILogger<NetworkListener> logger,
    INetworkClientFactory clientFactory,
    INetworkClientConnectionHandler connectionHandler)
    : IHostedService
{
    private readonly NetworkOptions _options = options.Value;
    private WebApplication? _app;

    private readonly ConcurrentDictionary<IPAddress, int> _connectionsPerAddress = new();
    private int _connections;

    internal bool TryReserveSlot(IPAddress ip)
    {
        var max = _options.MaxConnections;

        if (max > 0 && Interlocked.Increment(ref _connections) > max)
        {
            Interlocked.Decrement(ref _connections);
            return false;
        }

        var perAddress = _options.MaxConnectionsPerAddress;

        if (perAddress <= 0)
        {
            return true;
        }

        var accepted = false;

        while (true)
        {
            if (_connectionsPerAddress.TryGetValue(ip, out var count))
            {
                if (count >= perAddress)
                {
                    break;
                }

                if (_connectionsPerAddress.TryUpdate(ip, count + 1, count))
                {
                    accepted = true;
                    break;
                }
            }
            else if (_connectionsPerAddress.TryAdd(ip, 1))
            {
                accepted = true;
                break;
            }
        }

        if (!accepted && max > 0)
        {
            Interlocked.Decrement(ref _connections);
        }

        return accepted;
    }

    internal void ReleaseSlot(IPAddress ip)
    {
        if (_options.MaxConnections > 0)
        {
            Interlocked.Decrement(ref _connections);
        }

        if (_options.MaxConnectionsPerAddress <= 0)
        {
            return;
        }

        while (_connectionsPerAddress.TryGetValue(ip, out var count))
        {
            if (count <= 1)
            {
                if (_connectionsPerAddress.TryRemove(new KeyValuePair<IPAddress, int>(ip, count)))
                {
                    return;
                }

                continue;
            }

            if (_connectionsPerAddress.TryUpdate(ip, count - 1, count))
            {
                return;
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.ConfigureKestrel(k =>
        {
            var ip = IPAddress.Parse(_options.Host ?? "127.0.0.1");

            if (!_options.UseWss)
            {
                k.Listen(ip, _options.Port);
                return;
            }

            if (string.IsNullOrWhiteSpace(_options.CertificateFile))
            {
                throw new InvalidOperationException(
                    "Network:UseWss is enabled but Network:CertificateFile is not set.");
            }

            if (!File.Exists(_options.CertificateFile))
            {
                throw new InvalidOperationException(
                    $"Network:CertificateFile '{_options.CertificateFile}' was not found.");
            }

            k.Listen(ip, _options.Port, o => o.UseHttps(_options.CertificateFile));
        });

        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.ClearProviders();

        var app = builder.Build();

        app.UseWebSockets();

        app.Map("/", async ctx =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            var ip = ctx.Connection.RemoteIpAddress ?? IPAddress.None;

            if (!TryReserveSlot(ip))
            {
                logger.LogWarning("Rejected connection from {Ip}: connection limit reached", ip);
                ctx.Response.StatusCode = 503;

                return;
            }

            try
            {
                var socket = await ctx.WebSockets.AcceptWebSocketAsync();
                var guid = Guid.NewGuid();

                var client = clientFactory.CreateClient(ip, guid, socket);

                await connectionHandler.HandleClientAsync(client, ctx.RequestAborted);
            }
            finally
            {
                ReleaseSlot(ip);
            }
        });

        _app = app;

        return app.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _app?.StopAsync(cancellationToken) ?? Task.CompletedTask;
    }
}
