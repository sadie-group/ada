using Ada.Options.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ada.Networking.Encryption;

public static class EncryptionServiceProvider
{
    public static void AddServices(IServiceCollection serviceCollection, IConfiguration config)
    {
        serviceCollection.AddSingleton<HabboEncryption>();
        serviceCollection.Configure<EncryptionOptions>(options => config.GetSection("Encryption").Bind(options));
    }
}
