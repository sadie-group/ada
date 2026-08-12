using System.Reflection;
using System.Runtime.Loader;
using Ada.API.Interfaces.Game.Rooms.Chat.Commands;
using Ada.API.Interfaces.Game.Rooms.Furniture;
using Ada.API.Interfaces.Game.Rooms.Furniture.Processors;
using Ada.API.Interfaces.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ada.Server;

public static class ServiceCollectionHelpers
{
    extension(IServiceCollection serviceCollection)
    {
        public void RegisterRoomChatCommands(Assembly[]  assemblies)
        {
            serviceCollection.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<IRoomChatCommand>())
                .As<IRoomChatCommand>()
                .WithSingletonLifetime());
        }

        public void RegisterFurnitureInteractors(Assembly[]  assemblies)
        {
            serviceCollection.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<AbstractRoomFurnitureItemInteractor>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
        }

        public void RegisterRoomFurnitureProcessors(Assembly[]  assemblies)
        {
            serviceCollection.Scan(scan => scan
                .FromAssemblies(assemblies)
                .AddClasses(classes => classes.AssignableTo<IRoomFurnitureItemProcessor>())
                .AsImplementedInterfaces()
                .WithSingletonLifetime());
        }

        public void RegisterPluginServices(Assembly[]  assemblies)
        {
            var pluginServiceCollections =
                assemblies
                    .SelectMany(a => a.GetTypes())
                    .Where(t =>
                        typeof(IPluginServiceCollection).IsAssignableFrom(t) &&
                        t is { IsClass: true, IsAbstract: false })
                    .Select(t => Activator.CreateInstance(t) as IPluginServiceCollection)
                    .Where(x => x is not null)!
                    .ToList();

            foreach (var pluginServiceCollection in pluginServiceCollections)
            {
                pluginServiceCollection?.Register(serviceCollection);
            }
        }
    }

    public static void LoadPlugins(IConfiguration config)
    {
        var pluginFolder = config.GetValue<string>("PluginDirectory");

        if (string.IsNullOrEmpty(pluginFolder) || !Directory.Exists(pluginFolder))
        {
            Log.Warning($"Plugin folder not found: {pluginFolder}");
            return;
        }

        var allowed = config.GetSection("PluginAssemblies").Get<string[]>() ?? [];

        if (allowed.Length == 0)
        {
            Log.Warning(
                "Plugin folder '{Folder}' is configured but PluginAssemblies is empty, so no plugins " +
                "will be loaded. List the assembly file names you trust.", pluginFolder);

            return;
        }

        foreach (var name in allowed)
        {
            var fileName = Path.GetFileName(name);

            if (string.IsNullOrWhiteSpace(fileName) || !fileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                Log.Warning("Ignoring plugin entry '{Entry}': expected a bare '.dll' file name", name);
                continue;
            }

            var path = Path.Combine(pluginFolder, fileName);

            if (!File.Exists(path))
            {
                Log.Warning("Plugin '{Plugin}' is listed in PluginAssemblies but was not found", fileName);
                continue;
            }

            var fullPath = Path.GetFullPath(path);
            var actualHash = ComputeSha256(fullPath);
            var expectedHash = config.GetValue<string>($"PluginHashes:{fileName}");

            if (string.IsNullOrWhiteSpace(expectedHash))
            {
                Log.Warning(
                    "Plugin '{Plugin}' is not pinned. It replaces server behaviour and nothing verifies " +
                    "it has not been swapped. Add PluginHashes:{Plugin} = {Hash} to pin this build.",
                    fileName, fileName, actualHash);
            }
            else if (!string.Equals(expectedHash.Trim(), actualHash, StringComparison.OrdinalIgnoreCase))
            {
                Log.Error(
                    "Refusing to load plugin '{Plugin}': expected SHA-256 {Expected} but the file on disk " +
                    "is {Actual}. Either the plugin was rebuilt and the pin needs updating, or it was replaced.",
                    fileName, expectedHash.Trim(), actualHash);

                continue;
            }

            var context = new PluginLoadContext(fullPath);

            _pluginContexts.Add(context);

            var assembly = context.LoadFromAssemblyPath(fullPath);
            var version = assembly.GetName().Version;

            Console.WriteLine($"Loaded plugin: {Path.GetFileNameWithoutExtension(path)} {version}");
        }
    }

    private static readonly List<PluginLoadContext> _pluginContexts = [];

    public static IReadOnlyList<PluginLoadContext> PluginContexts => _pluginContexts;

    public sealed class PluginLoadContext(string pluginPath) : AssemblyLoadContext(
        name: $"Plugin:{Path.GetFileNameWithoutExtension(pluginPath)}",
        isCollectible: true)
    {
        private readonly AssemblyDependencyResolver _resolver = new(pluginPath);

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (Default.Assemblies.Any(x =>
                    string.Equals(x.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }

            var resolved = _resolver.ResolveAssemblyToPath(assemblyName);

            return resolved == null ? null : LoadFromAssemblyPath(resolved);
        }

        protected override nint LoadUnmanagedDll(string unmanagedDllName)
        {
            var resolved = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

            return resolved == null ? nint.Zero : LoadUnmanagedDllFromPath(resolved);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream));
    }
}
