using System.Net;
using System.Net.Sockets;

namespace Ada.Networking.Client;

public sealed class ClientAddressResolver
{
    private readonly (IPAddress Network, int PrefixLength)[] _trusted;

    public ClientAddressResolver(string? trustedProxies)
    {
        _trusted = Parse(trustedProxies);
    }

    public bool HasTrustedProxies => _trusted.Length > 0;

    private static (IPAddress Network, int PrefixLength)[] Parse(string? trustedProxies)
    {
        if (string.IsNullOrWhiteSpace(trustedProxies))
        {
            return [];
        }

        var parsed = new List<(IPAddress, int)>();

        foreach (var entry in trustedProxies.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var slash = entry.IndexOf('/');

            if (slash < 0)
            {
                if (IPAddress.TryParse(entry, out var single))
                {
                    parsed.Add((Normalise(single), single.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32));
                }

                continue;
            }

            if (IPAddress.TryParse(entry[..slash], out var network) &&
                int.TryParse(entry[(slash + 1)..], out var prefix))
            {
                var maxPrefix = network.AddressFamily == AddressFamily.InterNetworkV6 ? 128 : 32;

                if (prefix >= 0 && prefix <= maxPrefix)
                {
                    parsed.Add((Normalise(network), prefix));
                }
            }
        }

        return parsed.ToArray();
    }

    private static IPAddress Normalise(IPAddress address)
        => address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

    public bool IsTrustedProxy(IPAddress address)
    {
        var candidate = Normalise(address);

        foreach (var (network, prefixLength) in _trusted)
        {
            if (Contains(network, prefixLength, candidate))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(IPAddress network, int prefixLength, IPAddress candidate)
    {
        if (network.AddressFamily != candidate.AddressFamily)
        {
            return false;
        }

        var networkBytes = network.GetAddressBytes();
        var candidateBytes = candidate.GetAddressBytes();

        var wholeBytes = prefixLength / 8;
        var remainingBits = prefixLength % 8;

        for (var i = 0; i < wholeBytes; i++)
        {
            if (networkBytes[i] != candidateBytes[i])
            {
                return false;
            }
        }

        if (remainingBits == 0)
        {
            return true;
        }

        var mask = (byte) (0xFF << (8 - remainingBits));

        return (networkBytes[wholeBytes] & mask) == (candidateBytes[wholeBytes] & mask);
    }

    public IPAddress Resolve(IPAddress? peerAddress, string? forwardedFor)
    {
        var peer = Normalise(peerAddress ?? IPAddress.None);

        if (_trusted.Length == 0 || string.IsNullOrWhiteSpace(forwardedFor) || !IsTrustedProxy(peer))
        {
            return peer;
        }

        var hops = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        for (var i = hops.Length - 1; i >= 0; i--)
        {
            if (!IPAddress.TryParse(StripPort(hops[i]), out var hop))
            {
                break;
            }

            hop = Normalise(hop);

            if (!IsTrustedProxy(hop))
            {
                return hop;
            }
        }

        return peer;
    }

    private static string StripPort(string value)
    {
        if (value.StartsWith('['))
        {
            var close = value.IndexOf(']');
            return close > 0 ? value[1..close] : value;
        }

        var colon = value.IndexOf(':');

        return colon > 0 && value.IndexOf(':', colon + 1) < 0 ? value[..colon] : value;
    }
}
