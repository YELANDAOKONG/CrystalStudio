using Crystal.Chat;
using Crystal.Reasoning;

using CrystalCode.Configuration;
using CrystalCode.Home;
using CrystalCode.Plugins;

using CrystalStudio.Configuration;
using CrystalStudio.Interfaces;

namespace CrystalStudio.Adapters;

/// <summary>
/// Creates Crystal <see cref="IChatClient"/> instances from CrystalCode provider settings.
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
            var key = member.Provider + "/" + member.Model;
            if (_clients.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var harness = _settings.WithOverrides(member.Provider, member.Model);
            if (!_credentials.TryResolve(harness.ActiveProvider, out var apiKey, out var error))
            {
                throw new InvalidOperationException(error);
            }

            var client = _registry.CreateClient(harness, apiKey);
            _clients[key] = client;
            return client;
        }
    }

    public ReasoningOptions? ResolveReasoning(CouncilMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        var harness = _settings
            .WithOverrides(member.Provider, member.Model)
            .WithThinkingEffort(ThinkingSelection.Parse(member.Thinking));
        return harness.ResolveReasoning();
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
