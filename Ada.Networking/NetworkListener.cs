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

            var socket = await ctx.WebSockets.AcceptWebSocketAsync();
            var guid = Guid.NewGuid();
            var ip = ctx.Connection.RemoteIpAddress ?? IPAddress.None;

            var client = clientFactory.CreateClient(ip, guid, socket);

            await connectionHandler.HandleClientAsync(client, ctx.RequestAborted);
        });

        _app = app;

        return app.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _app?.StopAsync(cancellationToken) ?? Task.CompletedTask;
    }
}
