using Ada.Console.Services;
using Ada.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Ada.Console;

internal static class Program
{
    private static async Task Main()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ConsoleBannerService>();
                ServerServiceCollection.AddServices(services, context.Configuration);
                services.AddHostedService<ServerHostedService>();
            })
            .UseSerilog((hostContext, _, logger) => 
                logger.ReadFrom.Configuration(hostContext.Configuration))
            .Build();
        
        await host.RunAsync();
    }

    private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Logger.Error(e.ExceptionObject as Exception, "Unhandled exception");
    }
}