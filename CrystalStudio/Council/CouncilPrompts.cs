using System.Text;

namespace CrystalStudio.Council;

/// <summary>
/// English prompts the council sends through Crystal chat requests.
/// </summary>
public static class CouncilPrompts
{
    public static string ProposalSystem(string persona) =>
        $"{RequirePersona(persona)}\n\n"
        + "You are one isolated council member. Answer the user's request completely. "
        + "Do not mention other models, other members, or a council. "
        + "If tools are available and a tool call is the correct next step, request those tools. "
        + "When several independent reads are needed, request every needed read in one turn "
        + "rather than one file at a time. "
        + "Prefer dedicated read tools over a shell listing when you need file contents. "
        + "A directory listing is not a finished answer to a request for a document. "
        + "Otherwise return a finished text answer.";

    public static string ReviewSystem(string persona) =>
        $"{RequirePersona(persona)}\n\n"
        + "You are reviewing anonymous proposals. Rank them only by the written content. "
        + "Ignore any guess about which model wrote them. "
        + "Reply with JSON only, no markdown fences, using this shape:\n"
        + "{\"ranking\":[\"1\",\"2\"],\"risks\":[{\"id\":\"1\",\"level\":\"high\",\"note\":\"why\"}]}\n"
        + "ranking lists every proposal id from best to worst. "
        + "risks may be empty. level is low, medium, or high. "
        + "Use high only for destructive or unsafe side effects "
        + "(data loss, irreversible commands, leaked secrets). "
        + "An incomplete answer should rank lower; it is not a high safety risk.";

    public static string RevisionSystem(string persona) =>
        $"{RequirePersona(persona)}\n\n"
        + "You may revise your answer after seeing anonymous proposals and critiques. "
        + "Produce a complete replacement answer, not a commentary. "
        + "Do not mention other models. If tool results are now in the transcript, "
        + "write the requested document. If tool calls are still the right action, "
        + "request every needed tool in one turn.";

    public static string ChairSystem(string persona) =>
        $"{RequirePersona(persona)}\n\n"
        + "You are the chair. Confirm whether the leading original proposal is acceptable. "
        + "Do not rewrite it and do not invent a new answer. "
        + "Reply with JSON only: {\"accept\":true,\"explanation\":\"why this proposal stands\"}.";

    public static string ReviewUser(IReadOnlyList<LabeledProposal> labeled)
    {
        ArgumentNullException.ThrowIfNull(labeled);
        var builder = new StringBuilder();
        builder.AppendLine("Rank these anonymous proposals from best to worst.");
        foreach (var item in labeled)
        {
            builder.AppendLine();
            builder.Append("Proposal ").Append(item.Label).AppendLine(":");
            builder.AppendLine(Describe(item.Proposal));
        }

        return builder.ToString();
    }

    public static string RevisionUser(
        IReadOnlyList<LabeledProposal> labeled,
        IReadOnlyList<ReviewBallot> ballots)
    {
        ArgumentNullException.ThrowIfNull(labeled);
        ArgumentNullException.ThrowIfNull(ballots);
        var builder = new StringBuilder();
        builder.AppendLine("Anonymous proposals from the last round:");
        foreach (var item in labeled)
        {
            builder.AppendLine();
            builder.Append("Proposal ").Append(item.Label).AppendLine(":");
            builder.AppendLine(Describe(item.Proposal));
        }

        builder.AppendLine();
        builder.AppendLine("Anonymous reviews (rankings only):");
        foreach (var ballot in ballots)
        {
            builder.Append("Ranking: ").AppendLine(string.Join(", ", ballot.Ranking));
            foreach (var risk in ballot.Risks)
            {
                builder.Append("Risk on ").Append(risk.Label).Append(" (").Append(risk.Level)
                    .Append("): ").AppendLine(risk.Note);
            }
        }

        builder.AppendLine();
        builder.AppendLine("Write your revised complete answer now.");
        return builder.ToString();
    }

    public static string ChairUser(
        IReadOnlyList<LabeledProposal> labeled,
        IReadOnlyDictionary<string, int> scores,
        string winnerLabel)
    {
        ArgumentNullException.ThrowIfNull(labeled);
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentException.ThrowIfNullOrWhiteSpace(winnerLabel);

        var builder = new StringBuilder();
        builder.AppendLine("Borda scores (higher is better):");
        foreach (var (label, score) in scores.OrderByDescending(static pair => pair.Value))
        {
            builder.Append(label).Append('=').Append(score).AppendLine();
        }

        builder.AppendLine();
        builder.Append("Leading proposal is ").Append(winnerLabel).AppendLine(":");
        foreach (var item in labeled)
        {
            if (item.Label == winnerLabel)
            {
                builder.AppendLine(Describe(item.Proposal));
                break;
            }
        }

        builder.AppendLine();
        builder.AppendLine("Confirm this original proposal. Do not replace it.");
        return builder.ToString();
    }

    public static string Describe(Proposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        if (proposal.HasToolCall)
        {
            var builder = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(proposal.Text))
            {
                builder.AppendLine(proposal.Text);
            }

            foreach (var call in proposal.ToolCalls)
            {
                builder.Append("Tool call: ").Append(call.Name).AppendLine();
                builder.Append("Arguments: ").AppendLine(call.Arguments);
            }

            return builder.ToString().TrimEnd();
        }

        return string.IsNullOrWhiteSpace(proposal.Text)
            ? "(empty proposal)"
            : proposal.Text;
    }

    private static string RequirePersona(string persona)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(persona);
        return persona.Trim();
    }
}
