namespace CrystalStudio.Council;

/// <summary>
/// One member's structured ranking of anonymous proposals.
/// </summary>
public sealed record ReviewBallot
{
    public ReviewBallot(
        string reviewerId,
        IReadOnlyList<string> ranking,
        IReadOnlyList<ReviewRisk>? risks = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reviewerId);
        ArgumentNullException.ThrowIfNull(ranking);

        ReviewerId = reviewerId;
        Ranking = ranking;
        Risks = risks ?? [];
    }

    public string ReviewerId { get; }

    public IReadOnlyList<string> Ranking { get; }

    public IReadOnlyList<ReviewRisk> Risks { get; }

    public override string ToString() => ReviewerId;
}
