using Microsoft.Extensions.Options;

namespace Cachr.Core.Discovery;

public sealed class StaticPeerDiscoveryProvider : IPeerDiscoveryProvider
{
    private readonly IOptionsMonitor<StaticPeerConfiguration> _options;
    private StaticPeerConfiguration _staticPeerConfiguration;
    private readonly IDisposable _onChangeSubscription;

    public StaticPeerDiscoveryProvider(IOptionsMonitor<StaticPeerConfiguration> options)
    {
        _options = options;
        _onChangeSubscription = _options.OnChange(OnUrlListChanged);
        _staticPeerConfiguration = options.CurrentValue;
    }

    private void OnUrlListChanged(StaticPeerConfiguration configuration) =>
        Interlocked.Exchange(ref _staticPeerConfiguration, configuration);

    public Task<IEnumerable<string>> DiscoverPeersAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult<IEnumerable<string>>(_staticPeerConfiguration?.BootstrapUrls ?? Array.Empty<string>());
    }
}
