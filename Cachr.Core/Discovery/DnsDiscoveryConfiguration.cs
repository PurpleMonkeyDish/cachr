namespace Cachr.Core.Discovery;

public sealed class DnsDiscoveryConfiguration
{
    public string HostName { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
}
