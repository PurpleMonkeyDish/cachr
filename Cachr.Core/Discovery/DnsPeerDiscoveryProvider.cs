using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Options;

namespace Cachr.Core.Discovery;

public sealed class DnsPeerDiscoveryProvider : IPeerDiscoveryProvider, IDisposable
{
    private DnsDiscoveryConfiguration _dnsDiscoveryConfiguration;
    private readonly IDisposable _onChangeSubscription;

    public DnsPeerDiscoveryProvider(IOptionsMonitor<DnsDiscoveryConfiguration> options)
    {
        _onChangeSubscription = options.OnChange(OnConfigurationChanged);
        _dnsDiscoveryConfiguration = options.CurrentValue;
    }

    private void OnConfigurationChanged(DnsDiscoveryConfiguration dnsDiscoveryConfiguration)
    {
        Interlocked.Exchange(ref _dnsDiscoveryConfiguration, dnsDiscoveryConfiguration);
    }

    public async Task<IEnumerable<string>> DiscoverPeersAsync(CancellationToken cancellationToken)
    {
        if(string.IsNullOrWhiteSpace(_dnsDiscoveryConfiguration.HostName))
            return Enumerable.Empty<string>();
        var hostEntries = await Dns.GetHostAddressesAsync(_dnsDiscoveryConfiguration.HostName);
        if(hostEntries.Length == 0) return Enumerable.Empty<string>();
        return EnumerateHostEntriesToUri(hostEntries);
    }

    private IEnumerable<string> EnumerateHostEntriesToUri(IPAddress[] hostEntries)
    {
        foreach (var address in hostEntries.Where(i =>
                     i.AddressFamily == AddressFamily.InterNetwork
                     ))
        {
            yield return $"{address}:{_dnsDiscoveryConfiguration.Port}";
        }
    }

    public void Dispose()
    {
        _onChangeSubscription.Dispose();
    }
}
