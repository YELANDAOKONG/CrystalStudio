using Crystal;
using Crystal.Chat;
using Crystal.Tools;

using CrystalStudio.Configuration;
using CrystalStudio.Interfaces;

namespace CrystalStudio.Council;

/// <summary>
/// Runs the three-phase council protocol for one inbound chat request.
/// </summary>
public sealed class CouncilSession
{
    private const int ExcerptLength = 160;

    private readonly CouncilSettings _settings;
    private readonly IMemberClientFactory _clients;
    private readonly UsageTally _usage = new();

    public CouncilSession(CouncilSettings settings, IMemberClientFactory clients)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(clients);
        _settings = settings;
        _clients = clients;
    }

    public async Task<CouncilAction> RunAsync(
        ChatRequest request,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(observer);

        var transcript = new Transcript(request.Items);
        await observer.ReportAsync(
            $"Council opened with {_settings.Members.Count} members. "
            + "Phase 1 is isolated proposals.",
            cancellationToken);

        IReadOnlyList<LabeledProposal> labeled = [];
        IReadOnlyList<ReviewBallot> ballots = [];
        var seed = QuestionSeed(request.Items);

        for (var round = 1; round <= _settings.MaxRounds; round++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var proposals = round == 1
                ? await ProposeAsync(
                    request,
                    CouncilPrompts.ProposalSystem,
                    extraUser: null,
                    includeTools: true,
                    round,
                    observer,
                    cancellationToken)
                : await ProposeAsync(
                    request,
                    CouncilPrompts.RevisionSystem,
                    CouncilPrompts.RevisionUser(labeled, ballots),
                    includeTools: true,
                    round,
                    observer,
                    cancellationToken);

            foreach (var proposal in proposals)
            {
                transcript.AddProposal(proposal);
            }

            var latest = transcript.LatestProposals();
            if (latest.Count == 0)
            {
                await observer.ReportAsync(
                    "Every member abstained. The council cannot decide.",
                    cancellationToken);
                return Degraded(
                    "The council produced no usable proposals. Every member abstained or timed out.",
                    observer);
            }

            if (latest.Count == 1)
            {
                await observer.ReportAsync(
                    "Only one proposal remains. Skipping review and moving to the chair.",
                    cancellationToken);
                labeled = Anonymizer.Shuffle(latest, seed + round);
                ballots = [];
                break;
            }

            labeled = Anonymizer.Shuffle(latest, seed + round);
            await observer.ReportAsync(
                $"Phase 2: anonymous review of {labeled.Count} proposals "
                + $"(labels {string.Join(", ", labeled.Select(static item => item.Label))}).",
                cancellationToken);
            ballots = await ReviewAsync(request, labeled, observer, cancellationToken);
            foreach (var ballot in ballots)
            {
                transcript.AddBallot(ballot);
            }

            var similarity = LexicalSimilarity.AveragePairwise(
                latest.Select(static proposal => proposal.Fingerprint).ToList());
            await observer.ReportAsync(
                $"Average proposal similarity is {similarity:0.00}. "
                + $"Threshold is {_settings.ConvergenceThreshold:0.00}.",
                cancellationToken);

            if (ConsensusDetector.ShouldStop(
                    latest,
                    round,
                    _settings.MaxRounds,
                    _settings.ConvergenceThreshold))
            {
                await observer.ReportAsync(
                    "Debate will stop. Moving to adjudication.",
                    cancellationToken);
                break;
            }

            await observer.ReportAsync(
                "Proposals have not converged. Starting a revision round.",
                cancellationToken);
        }

        return await DecideAsync(labeled, ballots, observer, cancellationToken);
    }

    private async Task<IReadOnlyList<Proposal>> ProposeAsync(
        ChatRequest original,
        Func<string, string> systemFor,
        string? extraUser,
        bool includeTools,
        int round,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        await observer.ReportAsync(
            round == 1
                ? "Calling every member in parallel for an independent proposal."
                : "Calling every member in parallel for a revised proposal.",
            cancellationToken);

        var tasks = _settings.Members.Select(member =>
            InvokeAsync(
                member,
                BuildRequest(
                    original,
                    systemFor(member.Persona),
                    extraUser,
                    includeTools ? original.Tools : []),
                async (response, token) =>
                {
                    var proposal = ReadProposal(member.Id, round, response);
                    await observer.ReportAsync(
                        proposal.IsEmpty
                            ? $"{member.Id} abstained."
                            : $"{member.Id} submitted a "
                              + (proposal.HasToolCall ? "tool-call" : "text")
                              + $" proposal ({Excerpt(proposal.Fingerprint)}).",
                        token);
                    return proposal;
                },
                () => Task.FromResult(new Proposal(member.Id, round, string.Empty)),
                observer,
                cancellationToken));

        var proposals = await Task.WhenAll(tasks);
        return proposals.Where(static proposal => !proposal.IsEmpty).ToList();
    }

    private async Task<IReadOnlyList<ReviewBallot>> ReviewAsync(
        ChatRequest original,
        IReadOnlyList<LabeledProposal> labeled,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        var user = CouncilPrompts.ReviewUser(labeled);
        var tasks = _settings.Members.Select(member =>
            InvokeAsync(
                member,
                BuildRequest(original, CouncilPrompts.ReviewSystem(member.Persona), user, []),
                async (response, token) =>
                {
                    var text = ReadAssistantText(response);
                    var ballot = ReviewParser.TryParse(member.Id, text);
                    if (ballot is null)
                    {
                        await observer.ReportAsync(
                            $"{member.Id} returned an unreadable ranking.",
                            token);
                        return null;
                    }

                    await observer.ReportAsync(
                        $"{member.Id} ranked {string.Join(" > ", ballot.Ranking)}.",
                        token);
                    return ballot;
                },
                () => Task.FromResult<ReviewBallot?>(null),
                observer,
                cancellationToken));

        var results = await Task.WhenAll(tasks);
        return results.Where(static ballot => ballot is not null).Cast<ReviewBallot>().ToList();
    }

    private async Task<CouncilAction> DecideAsync(
        IReadOnlyList<LabeledProposal> labeled,
        IReadOnlyList<ReviewBallot> ballots,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        if (labeled.Count == 0)
        {
            return Degraded(
                "The council produced no usable proposals. Every member abstained or timed out.",
                observer);
        }

        var labels = labeled.Select(static item => item.Label).ToList();
        var rankings = ballots.Select(static ballot => ballot.Ranking).ToList();
        var scores = rankings.Count == 0
            ? labels.ToDictionary(static label => label, _ => 0)
            : BordaTally.Score(labels, rankings);
        var winnerLabel = BordaTally.Winner(scores);
        var winner = labeled.First(item => item.Label == winnerLabel).Proposal;
        var disputed = BordaTally.IsDisputed(scores);
        await observer.ReportAsync(
            "Phase 3: Borda scores are "
            + string.Join(", ", scores.Select(static pair => $"{pair.Key}={pair.Value}"))
            + $". Leading label is {winnerLabel}"
            + (disputed ? " (disputed)." : "."),
            cancellationToken);

        if (RiskClassifier.IsHighRisk(winner)
            && (disputed || RiskClassifier.MarkedHighRisk(ballots, winnerLabel)))
        {
            await observer.ReportAsync(
                "The leading proposal is a high-risk tool call with unresolved disagreement.",
                cancellationToken);
            return Degraded(BuildRiskMessage(winner, ballots, winnerLabel), observer);
        }

        var chairAccepted = await ConfirmChairAsync(
            labeled,
            scores,
            winnerLabel,
            observer,
            cancellationToken);
        if (!chairAccepted)
        {
            return Degraded(
                "The chair rejected the leading proposal. The council will not return that action."
                + Environment.NewLine
                + Environment.NewLine
                + CouncilPrompts.Describe(winner),
                observer);
        }

        await observer.ReportAsync(
            $"Council selected the original proposal labeled {winnerLabel}. "
            + FormatUsage(_usage.Snapshot()),
            cancellationToken);
        return winner.HasToolCall
            ? new CouncilAction(
                CouncilOutcome.ToolCall,
                winner.Text,
                winner.ToolCall,
                observer is ProgressLog log ? log.Text : string.Empty,
                _usage.Snapshot())
            : new CouncilAction(
                CouncilOutcome.Text,
                string.IsNullOrWhiteSpace(winner.Text)
                    ? CouncilPrompts.Describe(winner)
                    : winner.Text,
                reasoning: observer is ProgressLog progress ? progress.Text : string.Empty,
                usage: _usage.Snapshot());
    }

    private async Task<bool> ConfirmChairAsync(
        IReadOnlyList<LabeledProposal> labeled,
        IReadOnlyDictionary<string, int> scores,
        string winnerLabel,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        await observer.ReportAsync(
            $"{_settings.Chair.Id} is confirming the leading proposal.",
            cancellationToken);
        var request = new ChatRequest(
            [
                new ChatMessage(ChatRole.System, CouncilPrompts.ChairSystem(_settings.Chair.Persona)),
                new ChatMessage(
                    ChatRole.User,
                    CouncilPrompts.ChairUser(labeled, scores, winnerLabel))
            ]);

        var accepted = await InvokeAsync(
            _settings.Chair,
            request,
            async (response, token) =>
            {
                var text = ReadAssistantText(response);
                if (!ReviewParser.TryParseChair(text, out var accept, out var explanation))
                {
                    await observer.ReportAsync(
                        $"{_settings.Chair.Id} returned an unreadable confirmation. "
                        + "The Borda winner stands.",
                        token);
                    return true;
                }

                await observer.ReportAsync(
                    $"{_settings.Chair.Id} {(accept ? "accepted" : "rejected")} "
                    + $"the leading proposal. {Excerpt(explanation)}",
                    token);
                return accept;
            },
            async () =>
            {
                await observer.ReportAsync(
                    $"{_settings.Chair.Id} abstained. The Borda winner stands.",
                    cancellationToken);
                return true;
            },
            observer,
            cancellationToken);

        return accepted;
    }

    private async Task<T> InvokeAsync<T>(
        CouncilMember member,
        ChatRequest request,
        Func<ChatResponse, CancellationToken, Task<T>> onSuccess,
        Func<Task<T>> onAbstain,
        ICouncilObserver observer,
        CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_settings.MemberTimeout);
        try
        {
            var client = _clients.Create(member);
            var call = new ChatRequest(
                request.Items,
                request.Tools,
                _clients.ResolveReasoning(member));
            var response = await client.CompleteAsync(call, timeout.Token);
            _usage.Add(response.Usage);
            return await onSuccess(response, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await observer.ReportAsync($"{member.Id} timed out.", cancellationToken);
            return await onAbstain();
        }
        catch (Exception exception)
        {
            await observer.ReportAsync($"{member.Id} failed: {exception.Message}", cancellationToken);
            return await onAbstain();
        }
    }

    private static ChatRequest BuildRequest(
        ChatRequest original,
        string system,
        string? extraUser,
        IReadOnlyList<ToolDefinition> tools)
    {
        var items = new List<ChatItem>(original.Items.Count + 2)
        {
            new ChatMessage(ChatRole.System, system)
        };
        items.AddRange(original.Items);
        if (!string.IsNullOrWhiteSpace(extraUser))
        {
            items.Add(new ChatMessage(ChatRole.User, extraUser));
        }

        return new ChatRequest(items, tools);
    }

    private static Proposal ReadProposal(string memberId, int round, ChatResponse response)
    {
        var candidate = response.Candidates[0];
        var text = new System.Text.StringBuilder();
        ToolCall? call = null;
        foreach (var item in candidate.Items)
        {
            switch (item)
            {
                case ChatMessage message when message.Role == ChatRole.Assistant:
                    text.Append(message.Text);
                    break;
                case ToolCall tool:
                    call ??= tool;
                    break;
                default:
                    break;
            }
        }

        return new Proposal(memberId, round, text.ToString(), call);
    }

    private static string ReadAssistantText(ChatResponse response)
    {
        var text = new System.Text.StringBuilder();
        foreach (var item in response.Candidates[0].Items)
        {
            if (item is ChatMessage message && message.Role == ChatRole.Assistant)
            {
                text.Append(message.Text);
            }
        }

        return text.ToString();
    }

    private CouncilAction Degraded(string text, ICouncilObserver observer)
    {
        var reasoning = observer is ProgressLog log ? log.Text : string.Empty;
        return new CouncilAction(
            CouncilOutcome.Degraded,
            text,
            reasoning: reasoning,
            usage: _usage.Snapshot());
    }

    private static string FormatUsage(TokenUsage usage)
    {
        var text =
            $"Token usage: {usage.InputTokenCount} prompt, {usage.OutputTokenCount} completion, "
            + $"{usage.TotalTokenCount} total";
        if (usage.ReasoningTokenCount is { } reasoning)
        {
            text += $", {reasoning} reasoning";
        }

        return text + ".";
    }

    private static string BuildRiskMessage(
        Proposal winner,
        IReadOnlyList<ReviewBallot> ballots,
        string winnerLabel)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine(
            "The council will not execute this action automatically. "
            + "Members disagreed on a high-risk tool call.");
        builder.AppendLine();
        builder.AppendLine("Leading proposal:");
        builder.AppendLine(CouncilPrompts.Describe(winner));
        builder.AppendLine();
        builder.AppendLine("Reviewer risk notes:");
        var any = false;
        foreach (var ballot in ballots)
        {
            foreach (var risk in ballot.Risks)
            {
                if (risk.Label != winnerLabel)
                {
                    continue;
                }

                any = true;
                builder.Append("- ").Append(risk.Level).Append(": ").AppendLine(risk.Note);
            }
        }

        if (!any)
        {
            builder.AppendLine("- Rankings were close on a destructive tool call.");
        }

        builder.AppendLine();
        builder.AppendLine("Confirm this action explicitly if you still want it.");
        return builder.ToString();
    }

    private static int QuestionSeed(IReadOnlyList<ChatItem> items)
    {
        var hash = 17;
        foreach (var item in items)
        {
            var piece = item switch
            {
                ChatMessage message => message.Text,
                ToolCall call => call.Name + call.Arguments,
                ToolResult result => result.Text,
                _ => item.ToString()
            };
            hash = HashCode.Combine(hash, piece);
        }

        return hash;
    }

    private static string Excerpt(string text)
    {
        var compact = text.ReplaceLineEndings(" ").Trim();
        if (compact.Length <= ExcerptLength)
        {
            return compact;
        }

        return compact[..ExcerptLength] + "...";
    }
}
