using Crystal.Chat;

using CrystalStudio.Configuration;

namespace CrystalStudio.Interfaces;

/// <summary>
/// Builds a Crystal chat client for one council member.
/// </summary>
public interface IMemberClientFactory
{
    IChatClient Create(CouncilMember member);
}
