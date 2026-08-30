namespace CrystalStudio.Council;

/// <summary>
/// Strips member identity and shuffles proposal order for peer review.
/// </summary>
public static class Anonymizer
{
    public static IReadOnlyList<LabeledProposal> Shuffle(
        IReadOnlyList<Proposal> proposals,
        int seed)
    {
        ArgumentNullException.ThrowIfNull(proposals);
        if (proposals.Count == 0)
        {
            return [];
        }

        var copy = proposals.ToList();
        var random = new Random(seed);
        for (var index = copy.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (copy[index], copy[swap]) = (copy[swap], copy[index]);
        }

        var labeled = new List<LabeledProposal>(copy.Count);
        for (var index = 0; index < copy.Count; index++)
        {
            labeled.Add(new LabeledProposal((index + 1).ToString(), copy[index]));
        }

        return labeled;
    }

    public static IReadOnlyDictionary<string, Proposal> Index(
        IReadOnlyList<LabeledProposal> labeled)
    {
        ArgumentNullException.ThrowIfNull(labeled);
        var map = new Dictionary<string, Proposal>(StringComparer.Ordinal);
        foreach (var item in labeled)
        {
            map[item.Label] = item.Proposal;
        }

        return map;
    }
}
