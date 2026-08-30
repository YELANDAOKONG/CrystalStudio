namespace CrystalStudio.Configuration;

/// <summary>
/// One council seat: a persona plus the Harness provider and model that speak for it.
/// </summary>
public sealed record CouncilMember
{
    public CouncilMember(
        string id,
        string persona,
        string provider,
        string model,
        bool chair = false)
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
    }

    public string Id { get; }

    public string Persona { get; }

    public string Provider { get; }

    public string Model { get; }

    public bool Chair { get; }

    public override string ToString() => Id;
}
