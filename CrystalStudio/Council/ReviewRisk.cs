namespace CrystalStudio.Council;

/// <summary>
/// One reviewer's risk note against an anonymous proposal label.
/// </summary>
public sealed record ReviewRisk
{
    public ReviewRisk(string label, string level, string note)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        ArgumentNullException.ThrowIfNull(note);

        Label = label.Trim();
        Level = level.Trim().ToLowerInvariant();
        Note = note;
    }

    public string Label { get; }

    public string Level { get; }

    public string Note { get; }

    public bool IsHigh => Level is "high" or "critical";

    public override string ToString() => $"{Label}:{Level}";
}
