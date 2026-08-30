using Crystal.Chat;

using CrystalStudio.Configuration;
using CrystalStudio.Interfaces;

namespace CrystalStudio.Tests.Council;

internal sealed class ScriptedClientFactory : IMemberClientFactory
{
    private readonly Dictionary<string, IChatClient> _clients;

    public ScriptedClientFactory(IReadOnlyDictionary<string, IChatClient> clients)
    {
        _clients = new Dictionary<string, IChatClient>(clients, StringComparer.Ordinal);
    }

    public IChatClient Create(CouncilMember member)
    {
        ArgumentNullException.ThrowIfNull(member);
        if (_clients.TryGetValue(member.Id, out var client))
        {
            return client;
        }

        throw new InvalidOperationException($"No scripted client for '{member.Id}'.");
    }
}
