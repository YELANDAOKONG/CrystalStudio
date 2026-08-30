using Crystal.Tools;

namespace CrystalStudio.Council;

/// <summary>
/// One member's complete output for a single round.
/// </summary>
public sealed record Proposal
{
    public Proposal(
        string memberId,
        int round,
        string text,
        IReadOnlyList<ToolCall>? toolCalls = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId);
        if (round < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(round), round, "Round must be at least 1.");
        }

        ArgumentNullException.ThrowIfNull(text);

        MemberId = memberId;
        Round = round;
        Text = text;
        ToolCalls = toolCalls is null || toolCalls.Count == 0 ? [] : [.. toolCalls];
    }

    public string MemberId { get; }

    public int Round { get; }

    public string Text { get; }

    public IReadOnlyList<ToolCall> ToolCalls { get; }

    public bool HasToolCall => ToolCalls.Count > 0;

    public string Fingerprint =>
        ToolCalls.Count == 0
            ? Text
            : string.Join(
                "\n---\n",
                ToolCalls
                    .Select(static call => $"{call.Name}\n{call.Arguments}")
                    .OrderBy(static piece => piece, StringComparer.Ordinal));

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Text) && ToolCalls.Count == 0;

    public override string ToString() => $"{MemberId}:{Round}";
}
