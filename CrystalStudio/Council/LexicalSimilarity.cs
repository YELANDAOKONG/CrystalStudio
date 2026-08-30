namespace CrystalStudio.Council;

/// <summary>
/// Cosine similarity over bag-of-words term frequencies. No embedding API.
/// </summary>
public static class LexicalSimilarity
{
    public static double Cosine(string left, string right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        var leftVector = TermFrequencies(left);
        var rightVector = TermFrequencies(right);
        if (leftVector.Count == 0 || rightVector.Count == 0)
        {
            return 0;
        }

        var dot = 0.0;
        foreach (var (term, leftCount) in leftVector)
        {
            if (rightVector.TryGetValue(term, out var rightCount))
            {
                dot += leftCount * rightCount;
            }
        }

        var leftNorm = Norm(leftVector);
        var rightNorm = Norm(rightVector);
        if (leftNorm == 0 || rightNorm == 0)
        {
            return 0;
        }

        return dot / (leftNorm * rightNorm);
    }

    public static double AveragePairwise(IReadOnlyList<string> texts)
    {
        ArgumentNullException.ThrowIfNull(texts);
        if (texts.Count < 2)
        {
            return 1;
        }

        var total = 0.0;
        var pairs = 0;
        for (var left = 0; left < texts.Count; left++)
        {
            for (var right = left + 1; right < texts.Count; right++)
            {
                total += Cosine(texts[left], texts[right]);
                pairs++;
            }
        }

        return pairs == 0 ? 1 : total / pairs;
    }

    private static Dictionary<string, int> TermFrequencies(string text)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var start = -1;
        for (var index = 0; index <= text.Length; index++)
        {
            var done = index == text.Length;
            var character = done ? '\0' : text[index];
            var isToken = !done && char.IsLetterOrDigit(character);
            if (isToken)
            {
                if (start < 0)
                {
                    start = index;
                }

                continue;
            }

            if (start < 0)
            {
                continue;
            }

            var token = text[start..index].ToLowerInvariant();
            start = -1;
            if (token.Length == 0)
            {
                continue;
            }

            counts[token] = counts.GetValueOrDefault(token) + 1;
        }

        return counts;
    }

    private static double Norm(Dictionary<string, int> vector)
    {
        var sum = 0.0;
        foreach (var count in vector.Values)
        {
            sum += count * count;
        }

        return Math.Sqrt(sum);
    }
}
