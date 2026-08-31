using CrystalCode.Configuration;

namespace CrystalStudio.Configuration;

/// <summary>
/// One council seat: a persona plus the CrystalCode provider and model that speak for it.
/// </summary>
public sealed record CouncilMember
{
    public CouncilMember(
        string id,
        string persona,
        string provider,
        string model,
        bool chair = false,
        string? thinking = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(persona);
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        Id = id.Trim();
        Persona = persona.Trim();
        Provider = provider.Trim();
        Model = model.Trim();
        Chair = chair;
        Thinking = NormalizeThinking(thinking);
    }

    public string Id { get; }

    public string Persona { get; }

    public string Provider { get; }

    public string Model { get; }

    public bool Chair { get; }

    /// <summary>
    /// Host thinking gear for this seat: <c>default</c>, <c>off</c>, or a
    /// Crystal effort name. Models that cannot think omit the hint.
    /// </summary>
    public string Thinking { get; }

    public override string ToString() => Id;

    private static string NormalizeThinking(string? thinking)
    {
        if (string.IsNullOrWhiteSpace(thinking))
        {
            return ThinkingSelection.Default.Value;
        }

        return ThinkingSelection.Parse(thinking).Value;
    }
}
