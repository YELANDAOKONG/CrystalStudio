namespace CrystalStudio.Council;

/// <summary>
/// The kind of action the council returns to the caller.
/// </summary>
public sealed record CouncilOutcome
{
    public static CouncilOutcome Text { get; } = new("text");

    public static CouncilOutcome ToolCall { get; } = new("tool_call");

    public static CouncilOutcome Degraded { get; } = new("degraded");

    public CouncilOutcome(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value.Trim().ToLowerInvariant();
    }

    public string Value { get; }

    public override string ToString() => Value;
}
