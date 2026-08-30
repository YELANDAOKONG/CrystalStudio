using Crystal;
using Crystal.Tools;

namespace CrystalStudio.Council;

/// <summary>
/// The council's final product: one original proposal, or a degraded explanation.
/// </summary>
public sealed record CouncilAction
{
    public CouncilAction(
        CouncilOutcome outcome,
        string text,
        IReadOnlyList<ToolCall>? toolCalls = null,
        string reasoning = "",
        TokenUsage? usage = null)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(reasoning);

        var calls = toolCalls is null || toolCalls.Count == 0 ? [] : toolCalls.ToArray();

        if (outcome == CouncilOutcome.ToolCall && calls.Length == 0)
        {
            throw new ArgumentException(
                "A tool-call action requires at least one tool call.",
                nameof(toolCalls));
        }

        if (outcome != CouncilOutcome.ToolCall && calls.Length > 0)
        {
            throw new ArgumentException(
                "Only a tool-call action may carry tool calls.",
                nameof(toolCalls));
        }

        Outcome = outcome;
        Text = text;
        ToolCalls = calls;
        Reasoning = reasoning;
        Usage = usage ?? new TokenUsage(0, 0);
    }

    public CouncilOutcome Outcome { get; }

    public string Text { get; }

    public IReadOnlyList<ToolCall> ToolCalls { get; }

    public string Reasoning { get; }

    public TokenUsage Usage { get; }

    public override string ToString() => Outcome.Value;
}
