namespace Ada.Networking.Options;

public class NetworkOptions
{
    public string? Host { get; init; }
    public int Port { get; init; }
    public bool UseWss { get; init; }
    public string? CertificateFile { get; init; }
    public int MaxConnections { get; init; } = 5000;
    public int MaxConnectionsPerAddress { get; init; } = 20;
}
