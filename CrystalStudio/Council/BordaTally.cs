namespace CrystalStudio.Council;

/// <summary>
/// Rank-order aggregation. First place earns n-1 points, last earns 0.
/// </summary>
public static class BordaTally
{
    public static IReadOnlyDictionary<string, int> Score(
        IReadOnlyList<string> labels,
        IReadOnlyList<IReadOnlyList<string>> rankings)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(rankings);

        var scores = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var label in labels)
        {
            scores[label] = 0;
        }

        foreach (var ranking in rankings)
        {
            ArgumentNullException.ThrowIfNull(ranking);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var points = labels.Count - 1;
            foreach (var raw in ranking)
            {
                var label = Normalize(raw);
                if (!scores.ContainsKey(label) || !seen.Add(label))
                {
                    continue;
                }

                scores[label] += points;
                points--;
            }
        }

        return scores;
    }

    public static string Winner(IReadOnlyDictionary<string, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        if (scores.Count == 0)
        {
            throw new ArgumentException("Scores cannot be empty.", nameof(scores));
        }

        string? winner = null;
        var best = int.MinValue;
        foreach (var (label, score) in scores)
        {
            if (winner is null || score > best || (score == best && CompareLabel(label, winner) < 0))
            {
                winner = label;
                best = score;
            }
        }

        return winner!;
    }

    public static bool IsDisputed(IReadOnlyDictionary<string, int> scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        if (scores.Count < 2)
        {
            return false;
        }

        var ordered = scores.Values.OrderByDescending(static value => value).ToList();
        return ordered[0] - ordered[1] <= 1;
    }

    private static string Normalize(string raw)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(raw);
        var trimmed = raw.Trim();
        var digits = new char[trimmed.Length];
        var count = 0;
        foreach (var character in trimmed)
        {
            if (char.IsDigit(character))
            {
                digits[count] = character;
                count++;
            }
        }

        return count == 0 ? trimmed : new string(digits, 0, count).TrimStart('0') switch
        {
            "" => "0",
            var value => value
        };
    }

    private static int CompareLabel(string left, string right)
    {
        if (int.TryParse(left, out var leftNumber) && int.TryParse(right, out var rightNumber))
        {
            return leftNumber.CompareTo(rightNumber);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }
}
