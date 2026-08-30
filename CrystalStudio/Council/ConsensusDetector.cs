namespace CrystalStudio.Council;

/// <summary>
/// Stops debate when proposals converge or the round cap is reached.
/// </summary>
public static class ConsensusDetector
{
    public static bool ShouldStop(
        IReadOnlyList<Proposal> proposals,
        int completedReviews,
        int maxRounds,
        double threshold)
    {
        ArgumentNullException.ThrowIfNull(proposals);
        if (completedReviews >= maxRounds)
        {
            return true;
        }

        if (proposals.Count < 2)
        {
            return true;
        }

        var texts = new List<string>(proposals.Count);
        foreach (var proposal in proposals)
        {
            texts.Add(proposal.Fingerprint);
        }

        return LexicalSimilarity.AveragePairwise(texts) >= threshold;
    }
}
