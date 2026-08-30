using System.Text.Json;

namespace CrystalStudio.Council;

/// <summary>
/// Reads a structured ranking from a member's review text.
/// </summary>
public static class ReviewParser
{
    public static ReviewBallot? TryParse(string reviewerId, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewerId);
        ArgumentNullException.ThrowIfNull(text);

        if (!TryExtractObject(text, out var json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!TryReadRanking(root, out var ranking) || ranking.Count == 0)
            {
                return null;
            }

            var risks = ReadRisks(root);
            return new ReviewBallot(reviewerId, ranking, risks);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryParseChair(string text, out bool accept, out string explanation)
    {
        ArgumentNullException.ThrowIfNull(text);
        accept = true;
        explanation = string.Empty;
        if (!TryExtractObject(text, out var json))
        {
            return false;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (root.TryGetProperty("accept", out var acceptNode)
                && acceptNode.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                accept = acceptNode.GetBoolean();
            }

            if (root.TryGetProperty("explanation", out var note)
                && note.ValueKind == JsonValueKind.String)
            {
                explanation = note.GetString() ?? string.Empty;
            }

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryReadRanking(JsonElement root, out List<string> ranking)
    {
        ranking = [];
        if (!root.TryGetProperty("ranking", out var node)
            || node.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var item in node.EnumerateArray())
        {
            var label = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Number => item.GetRawText(),
                JsonValueKind.Object when item.TryGetProperty("id", out var id)
                    && id.ValueKind == JsonValueKind.String => id.GetString(),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(label))
            {
                ranking.Add(label.Trim());
            }
        }

        return ranking.Count > 0;
    }

    private static List<ReviewRisk> ReadRisks(JsonElement root)
    {
        if (!root.TryGetProperty("risks", out var node)
            || node.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var risks = new List<ReviewRisk>();
        foreach (var item in node.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var label = ReadString(item, "id") ?? ReadString(item, "label");
            if (string.IsNullOrWhiteSpace(label))
            {
                continue;
            }

            var level = ReadString(item, "level") ?? "medium";
            var note = ReadString(item, "note") ?? string.Empty;
            risks.Add(new ReviewRisk(label, level, note));
        }

        return risks;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var node)
            || node.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return node.GetString();
    }

    private static bool TryExtractObject(string text, out string json)
    {
        json = string.Empty;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return false;
        }

        json = text[start..(end + 1)];
        return true;
    }
}
