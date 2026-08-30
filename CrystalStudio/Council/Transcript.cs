using Crystal.Chat;

namespace CrystalStudio.Council;

/// <summary>
/// Accumulated council state for one inbound request.
/// </summary>
public sealed class Transcript
{
    private readonly List<Proposal> _proposals = [];
    private readonly List<ReviewBallot> _ballots = [];

    public Transcript(IReadOnlyList<ChatItem> question)
    {
        ArgumentNullException.ThrowIfNull(question);
        Question = question;
    }

    public IReadOnlyList<ChatItem> Question { get; }

    public IReadOnlyList<Proposal> Proposals => _proposals;

    public IReadOnlyList<ReviewBallot> Ballots => _ballots;

    public void AddProposal(Proposal proposal)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        _proposals.Add(proposal);
    }

    public void AddBallot(ReviewBallot ballot)
    {
        ArgumentNullException.ThrowIfNull(ballot);
        _ballots.Add(ballot);
    }

    public IReadOnlyList<Proposal> LatestProposals()
    {
        if (_proposals.Count == 0)
        {
            return [];
        }

        var latestRound = 0;
        foreach (var proposal in _proposals)
        {
            if (proposal.Round > latestRound)
            {
                latestRound = proposal.Round;
            }
        }

        var latest = new List<Proposal>();
        foreach (var proposal in _proposals)
        {
            if (proposal.Round == latestRound && !proposal.IsEmpty)
            {
                latest.Add(proposal);
            }
        }

        return latest;
    }
}
