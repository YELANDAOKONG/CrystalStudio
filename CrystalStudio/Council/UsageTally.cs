using Crystal;

namespace CrystalStudio.Council;

/// <summary>
/// Thread-safe sum of provider-reported token usage across council calls.
/// </summary>
public sealed class UsageTally
{
    private readonly object _gate = new();
    private long _input;
    private long _output;
    private long _reasoning;
    private bool _hasReasoning;

    public void Add(TokenUsage? usage)
    {
        if (usage is null)
        {
            return;
        }

        lock (_gate)
        {
            _input = checked(_input + usage.InputTokenCount);
            _output = checked(_output + usage.OutputTokenCount);
            if (usage.ReasoningTokenCount is { } reasoning)
            {
                _reasoning = checked(_reasoning + reasoning);
                _hasReasoning = true;
            }
        }
    }

    public TokenUsage Snapshot()
    {
        lock (_gate)
        {
            return new TokenUsage(_input, _output, _hasReasoning ? _reasoning : null);
        }
    }
}
