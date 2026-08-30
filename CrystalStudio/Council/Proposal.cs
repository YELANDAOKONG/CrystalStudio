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
        ToolCall? toolCall = null)
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
        ToolCall = toolCall;
    }

    public string MemberId { get; }

    public int Round { get; }

    public string Text { get; }

    public ToolCall? ToolCall { get; }

    public bool HasToolCall => ToolCall is not null;

    public string Fingerprint =>
        ToolCall is null
            ? Text
            : $"{ToolCall.Name}\n{ToolCall.Arguments}";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Text) && ToolCall is null;

    public override string ToString() => $"{MemberId}:{Round}";
}
