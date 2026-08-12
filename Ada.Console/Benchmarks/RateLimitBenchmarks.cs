using System.Net;
using Ada.Core.Shared;
using BenchmarkDotNet.Attributes;

namespace Ada.Console.Benchmarks;

[MemoryDiagnoser]
public class RateLimitBenchmarks
{
    private KeyedTokenBucket<long> _playerBuckets = null!;
    private KeyedTokenBucket<IPAddress> _addressBuckets = null!;
    private IPAddress[] _addresses = null!;

    [Params(1_000, 10_000)]
    public int Keys { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _playerBuckets = new KeyedTokenBucket<long>(60, 40, TimeSpan.FromMinutes(2));
        _addressBuckets = new KeyedTokenBucket<IPAddress>(120, 60, TimeSpan.FromMinutes(2));

        _addresses = Enumerable.Range(0, Keys)
            .Select(i => new IPAddress(new byte[] { 10, (byte) (i >> 16), (byte) (i >> 8), (byte) i }))
            .ToArray();

        for (var i = 0; i < Keys; i++)
        {
            _playerBuckets.TryConsume(i);
            _addressBuckets.TryConsume(_addresses[i]);
        }
    }

    [Benchmark(Description = "Per-player token consume across the active key set")]
    public int ConsumeByPlayer()
    {
        var allowed = 0;

        for (var i = 0; i < Keys; i++)
        {
            if (_playerBuckets.TryConsume(i))
            {
                allowed++;
            }
        }

        return allowed;
    }

    [Benchmark(Description = "Per-address token consume (IPAddress hashing on the hot path)")]
    public int ConsumeByAddress()
    {
        var allowed = 0;

        foreach (var address in _addresses)
        {
            if (_addressBuckets.TryConsume(address))
            {
                allowed++;
            }
        }

        return allowed;
    }
}
