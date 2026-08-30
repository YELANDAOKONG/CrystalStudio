using Crystal.Chat;

using CrystalHarness.Configuration;
using CrystalHarness.Home;
using CrystalHarness.Plugins;

using CrystalStudio.Configuration;
using CrystalStudio.Interfaces;

namespace CrystalStudio.Adapters;

/// <summary>
/// Creates Crystal <see cref="IChatClient"/> instances from Harness provider settings.
/// </summary>
public sealed class MemberClientFactory : IMemberClientFactory, IDisposable
{
    private readonly HarnessSettings _settings;
    private readonly CredentialStore _credentials;
    private readonly PluginRegistry _registry;
    private readonly Dictionary<string, IChatClient> _clients;
    private readonly object _gate = new();

    public MemberClientFactory(
        HarnessSettings settings,
        CredentialStore credentials,
        PluginRegistry? registry = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(credentials);

        _settings = settings;
        _credentials = credentials;
        _registry = registry ?? PluginRegistry.CreateBuiltIn();
        _clients = new Dictionary<string, IChatClient>(StringComparer.Ordinal);
    }

    public IChatClient Create(CouncilMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        lock (_gate)
        {
            if (_clients.TryGetValue(member.Id, out var existing))
            {
                return existing;
            }

            var harness = _settings.WithOverrides(member.Provider, member.Model);
            if (!_credentials.TryResolve(harness.ActiveProvider, out var apiKey, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var client = _registry.CreateClient(harness, apiKey);
            _clients[member.Id] = client;
            return client;
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            foreach (var client in _clients.Values)
            {
                if (client is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _clients.Clear();
        }
    }
}
