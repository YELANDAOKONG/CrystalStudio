using Crystal.Chat;
using Crystal.Reasoning;

using CrystalStudio.Configuration;

namespace CrystalStudio.Interfaces;

/// <summary>
/// Builds a Crystal chat client for one council member.
/// </summary>
public interface IMemberClientFactory
{
    IChatClient Create(CouncilMember member);

    ReasoningOptions? ResolveReasoning(CouncilMember member);
}
