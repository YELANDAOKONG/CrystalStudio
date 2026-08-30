namespace CrystalStudio.Configuration;

/// <summary>
/// One named council: advertised model id, debate rules, and seats.
/// Provider catalogs and API keys come from Harness.
/// </summary>
public sealed record CouncilSettings
{
    public const int DefaultMaxRounds = 2;
    public const double DefaultConvergenceThreshold = 0.85;
    public const int DefaultMemberTimeoutSeconds = 180;
    public const string DefaultAdvertisedModel = "crystal-council";
    public const string DefaultWritingModel = "crystal-writing";
    public const string DefaultProvider = "deepseek";
    public const string DefaultModel = "deepseek-v4-flash";

    public CouncilSettings(
        int maxRounds,
        double convergenceThreshold,
        TimeSpan memberTimeout,
        string advertisedModel,
        IReadOnlyList<CouncilMember> members)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(advertisedModel);
        ArgumentNullException.ThrowIfNull(members);

        if (maxRounds < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRounds),
                maxRounds,
                "Maximum rounds must be at least 1.");
        }

        if (convergenceThreshold is <= 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(convergenceThreshold),
                convergenceThreshold,
                "Convergence threshold must be greater than 0 and at most 1.");
        }

        if (memberTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(memberTimeout),
                memberTimeout,
                "Member timeout must be positive.");
        }

        if (members.Count < 2)
        {
            throw new ArgumentException(
                "The council needs at least two members.",
                nameof(members));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in members)
        {
            ArgumentNullException.ThrowIfNull(member);
            if (!seen.Add(member.Id))
            {
                throw new ArgumentException(
                    $"Member id '{member.Id}' is listed more than once.",
                    nameof(members));
            }
        }

        MaxRounds = maxRounds;
        ConvergenceThreshold = convergenceThreshold;
        MemberTimeout = memberTimeout;
        AdvertisedModel = advertisedModel.Trim();
        Members = members;
        Chair = ResolveChair(members);
    }

    public int MaxRounds { get; }

    public double ConvergenceThreshold { get; }

    public TimeSpan MemberTimeout { get; }

    public string AdvertisedModel { get; }

    public IReadOnlyList<CouncilMember> Members { get; }

    public CouncilMember Chair { get; }

    public static CouncilSettings CreateDefault() =>
        Create(DefaultAdvertisedModel, CreateCodingMembers());

    public static CouncilSettings CreateWritingDefault() =>
        Create(DefaultWritingModel, CreateWritingMembers());

    public override string ToString() => AdvertisedModel;

    private static CouncilSettings Create(
        string advertisedModel,
        IReadOnlyList<CouncilMember> members)
    {
        return new CouncilSettings(
            DefaultMaxRounds,
            DefaultConvergenceThreshold,
            TimeSpan.FromSeconds(DefaultMemberTimeoutSeconds),
            advertisedModel,
            members);
    }

    private static CouncilMember ResolveChair(IReadOnlyList<CouncilMember> members)
    {
        foreach (var member in members)
        {
            if (member.Chair)
            {
                return member;
            }
        }

        return members[^1];
    }

    private static IReadOnlyList<CouncilMember> CreateCodingMembers() =>
    [
        new(
            "analyst",
            "You are a careful analyst. Structure the problem, list assumptions, "
            + "and prefer precise answers over rhetoric.",
            DefaultProvider,
            DefaultModel),
        new(
            "skeptic",
            "You are a skeptic. Hunt for holes, missing constraints, and overconfident claims. "
            + "Do not accept a plan just because it sounds fluent.",
            DefaultProvider,
            DefaultModel),
        new(
            "engineer",
            "You are a practical engineer. Prefer complete, executable answers. "
            + "Call out operational cost, failure modes, and what you would actually ship.",
            DefaultProvider,
            DefaultModel),
        new(
            "chair",
            "You are the council chair. Confirm whether the leading proposal is acceptable. "
            + "Do not invent a new answer. Explain the choice in plain language.",
            DefaultProvider,
            DefaultModel,
            chair: true)
    ];

    private static IReadOnlyList<CouncilMember> CreateWritingMembers() =>
    [
        new(
            "architect",
            "You are a book architect. Shape structure, argument, chapter flow, "
            + "and what the reader must understand at each beat. "
            + "Prefer a complete outline or draft over fragments.",
            DefaultProvider,
            DefaultModel),
        new(
            "stylist",
            "You are a prose stylist. Attend to voice, rhythm, diction, "
            + "and whether the sentences earn their length. "
            + "Cut decoration that does not serve the piece.",
            DefaultProvider,
            DefaultModel),
        new(
            "critic",
            "You are a demanding reader. Hunt cliches, unsupported claims, "
            + "a sagging middle, and places a reader would skim or get lost. "
            + "Do not praise fluency that hides emptiness.",
            DefaultProvider,
            DefaultModel),
        new(
            "chair",
            "You are the council chair. Confirm whether the leading proposal is acceptable. "
            + "Do not invent a new draft. Explain the choice in plain language.",
            DefaultProvider,
            DefaultModel,
            chair: true)
    ];
}
