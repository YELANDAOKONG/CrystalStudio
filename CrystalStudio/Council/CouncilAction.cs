using Crystal;
using Crystal.Tools;

namespace CrystalStudio.Council;

/// <summary>
/// The council's final product: one original proposal, or a degraded confirmation request.
/// </summary>
public sealed record CouncilAction
{
    public CouncilAction(
        CouncilOutcome outcome,
        string text,
        ToolCall? toolCall = null,
        string reasoning = "",
        TokenUsage? usage = null)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(reasoning);

        if (outcome == CouncilOutcome.ToolCall && toolCall is null)
        {
            throw new ArgumentException(
                "A tool-call action requires a tool call.",
                nameof(toolCall));
        }

        if (outcome != CouncilOutcome.ToolCall && toolCall is not null)
        {
            throw new ArgumentException(
                "Only a tool-call action may carry a tool call.",
                nameof(toolCall));
        }

        Outcome = outcome;
        Text = text;
        ToolCall = toolCall;
        Reasoning = reasoning;
        Usage = usage ?? new TokenUsage(0, 0);
    }

    public CouncilOutcome Outcome { get; }

    public string Text { get; }

    public ToolCall? ToolCall { get; }

    public string Reasoning { get; }

    public TokenUsage Usage { get; }

    public override string ToString() => Outcome.Value;
}
