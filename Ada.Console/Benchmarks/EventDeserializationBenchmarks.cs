using System.Runtime.CompilerServices;
using Ada.API.Interfaces.Networking.Client;
using Ada.API.Interfaces.Networking.Events.Handlers;
using Ada.API.Interfaces.Networking.Packets;
using Ada.Networking;
using Ada.Networking.Events.Generated;
using Ada.Networking.Events.Handlers.Rooms;
using Ada.Networking.Packets;
using BenchmarkDotNet.Attributes;

namespace Ada.Console.Benchmarks;

[MemoryDiagnoser]
public class EventDeserializationBenchmarks
{
    public sealed class SmallHandler : INetworkPacketEventHandler
    {
        public int ItemId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;

        public void ReadManually(INetworkPacketReader reader)
        {
            ItemId = reader.ReadInt();
            X = reader.ReadInt();
            Y = reader.ReadInt();
        }
    }

    public sealed class WideHandler : INetworkPacketEventHandler
    {
        public int RoomId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public int AccessType { get; set; }
        public string Password { get; set; } = "";
        public int MaxUsers { get; set; }
        public int CategoryId { get; set; }
        public int TradeOption { get; set; }
        public bool AllowPets { get; set; }
        public bool CanPetsEat { get; set; }
        public bool CanUsersOverlap { get; set; }
        public bool HideWall { get; set; }

        public Task HandleAsync(INetworkClient client) => Task.CompletedTask;

        public void ReadManually(INetworkPacketReader reader)
        {
            RoomId = reader.ReadInt();
            Name = reader.ReadString();
            Description = reader.ReadString();
            AccessType = reader.ReadInt();
            Password = reader.ReadString();
            MaxUsers = reader.ReadInt();
            CategoryId = reader.ReadInt();
            TradeOption = reader.ReadInt();
            AllowPets = reader.ReadBool();
            CanPetsEat = reader.ReadBool();
            CanUsersOverlap = reader.ReadBool();
            HideWall = reader.ReadBool();
        }
    }

    private byte[] _smallBody = null!;
    private byte[] _wideBody = null!;
    private byte[] _settingsBody = null!;
    private RoomSettingsSaveEventHandler _settingsHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        _settingsHandler = (RoomSettingsSaveEventHandler) RuntimeHelpers.GetUninitializedObject(typeof(RoomSettingsSaveEventHandler));

        var settings = new NetworkPacketWriter();
        settings.WriteInteger(1);
        settings.WriteString("my room");
        settings.WriteString("a description");
        settings.WriteInteger(0);
        settings.WriteString("");
        settings.WriteInteger(25);
        settings.WriteInteger(1);
        settings.WriteInteger(2);
        settings.WriteString("tag1");
        settings.WriteString("tag2");
        settings.WriteInteger(1);
        settings.WriteBool(true);
        settings.WriteBool(true);
        settings.WriteBool(false);
        settings.WriteBool(false);
        settings.WriteInteger(0);
        settings.WriteInteger(0);
        settings.WriteInteger(1);
        settings.WriteInteger(1);
        settings.WriteInteger(1);
        settings.WriteInteger(2);
        settings.WriteInteger(3);
        settings.WriteInteger(4);
        settings.WriteInteger(5);
        settings.WriteInteger(6);
        _settingsBody = BodyOf(settings);

        var small = new NetworkPacketWriter();
        small.WriteInteger(100);
        small.WriteInteger(5);
        small.WriteInteger(6);
        _smallBody = BodyOf(small);

        var wide = new NetworkPacketWriter();
        wide.WriteInteger(1);
        wide.WriteString("my room");
        wide.WriteString("a description");
        wide.WriteInteger(0);
        wide.WriteString("");
        wide.WriteInteger(25);
        wide.WriteInteger(1);
        wide.WriteInteger(0);
        wide.WriteBool(true);
        wide.WriteBool(true);
        wide.WriteBool(false);
        wide.WriteBool(false);
        _wideBody = BodyOf(wide);
    }

    private static byte[] BodyOf(NetworkPacketWriter writer)
    {
        var all = writer.GetAllBytes();

        return all.AsSpan(4).ToArray();
    }

    [Benchmark(Baseline = true, Description = "3 fields - reflected property fill")]
    public SmallHandler SmallReflected()
    {
        var handler = new SmallHandler();
        EventSerializer.SetPropertiesForEventHandler(handler, new NetworkPacketReader(_smallBody));

        return handler;
    }

    [Benchmark(Description = "3 fields - manual reads")]
    public SmallHandler SmallManual()
    {
        var handler = new SmallHandler();
        handler.ReadManually(new NetworkPacketReader(_smallBody));

        return handler;
    }

    [Benchmark(Description = "12 fields - reflected property fill")]
    public WideHandler WideReflected()
    {
        var handler = new WideHandler();
        EventSerializer.SetPropertiesForEventHandler(handler, new NetworkPacketReader(_wideBody));

        return handler;
    }

    [Benchmark(Description = "12 fields - manual reads")]
    public WideHandler WideManual()
    {
        var handler = new WideHandler();
        handler.ReadManually(new NetworkPacketReader(_wideBody));

        return handler;
    }

    [Benchmark(Description = "23 fields (real handler) - reflected property fill")]
    public RoomSettingsSaveEventHandler RealReflected()
    {
        EventSerializer.FastFill = null;
        EventSerializer.SetPropertiesForEventHandler(_settingsHandler, new NetworkPacketReader(_settingsBody));

        return _settingsHandler;
    }

    [Benchmark(Description = "23 fields (real handler) - source-generated fast path")]
    public RoomSettingsSaveEventHandler RealGenerated()
    {
        EventSerializer.FastFill = GeneratedPacketReaders.TryFill;
        EventSerializer.SetPropertiesForEventHandler(_settingsHandler, new NetworkPacketReader(_settingsBody));

        return _settingsHandler;
    }
}
