using Crystal.Tools;

namespace CrystalStudio.Council;

/// <summary>
/// Flags proposals whose tool calls have destructive side effects.
/// </summary>
public static class RiskClassifier
{
    private static readonly string[] HighRiskNames =
    [
        "bash",
        "shell",
        "exec",
        "execute",
        "write",
        "edit",
        "delete",
        "remove",
        "apply_patch"
    ];

    private static readonly string[] HighRiskFragments =
    [
        "rm ",
        "rm\t",
        "sudo ",
        "unlink ",
        "rmdir ",
        "del ",
        "format ",
        "drop table",
        "--force",
        "git push --force",
        "mkfs"
    ];

    public static bool IsHighRisk(Proposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return proposal.ToolCall is not null && IsHighRisk(proposal.ToolCall);
    }

    public static bool IsHighRisk(ToolCall call)
    {
        ArgumentNullException.ThrowIfNull(call);
        var name = call.Name.Trim();
        foreach (var risky in HighRiskNames)
        {
            if (string.Equals(name, risky, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        var arguments = call.Arguments;
        foreach (var fragment in HighRiskFragments)
        {
            if (arguments.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static bool MarkedHighRisk(IReadOnlyList<ReviewBallot> ballots, string label)
    {
        ArgumentNullException.ThrowIfNull(ballots);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        foreach (var ballot in ballots)
        {
            foreach (var risk in ballot.Risks)
            {
                if (string.Equals(risk.Label, label, StringComparison.Ordinal) && risk.IsHigh)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
