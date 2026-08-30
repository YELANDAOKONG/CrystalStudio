namespace CrystalStudio.Configuration;

/// <summary>
/// Council-owned settings. Provider catalogs and API keys come from Harness.
/// </summary>
public sealed record CouncilSettings
{
    public const int DefaultPort = 18790;
    public const int DefaultMaxRounds = 2;
    public const double DefaultConvergenceThreshold = 0.85;
    public const int DefaultMemberTimeoutSeconds = 180;
    public const string DefaultAdvertisedModel = "crystal-council";
    public const string DefaultProvider = "deepseek";
    public const string DefaultModel = "deepseek-v4-flash";

    public CouncilSettings(
        Uri listenPrefix,
        int maxRounds,
        double convergenceThreshold,
        TimeSpan memberTimeout,
        string advertisedModel,
        IReadOnlyList<CouncilMember> members)
    {
        ArgumentNullException.ThrowIfNull(listenPrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(advertisedModel);
        ArgumentNullException.ThrowIfNull(members);

        if (!listenPrefix.IsAbsoluteUri)
        {
            throw new ArgumentException("Listen prefix must be absolute.", nameof(listenPrefix));
        }

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

        ListenPrefix = listenPrefix;
        MaxRounds = maxRounds;
        ConvergenceThreshold = convergenceThreshold;
        MemberTimeout = memberTimeout;
        AdvertisedModel = advertisedModel.Trim();
        Members = members;
        Chair = ResolveChair(members);
    }

    public Uri ListenPrefix { get; }

    public int MaxRounds { get; }

    public double ConvergenceThreshold { get; }

    public TimeSpan MemberTimeout { get; }

    public string AdvertisedModel { get; }

    public IReadOnlyList<CouncilMember> Members { get; }

    public CouncilMember Chair { get; }

    public int Port => ListenPrefix.IsDefaultPort ? DefaultPort : ListenPrefix.Port;

    public static CouncilSettings CreateDefault()
    {
        return new CouncilSettings(
            new Uri($"http://127.0.0.1:{DefaultPort}/"),
            DefaultMaxRounds,
            DefaultConvergenceThreshold,
            TimeSpan.FromSeconds(DefaultMemberTimeoutSeconds),
            DefaultAdvertisedModel,
            CreateDefaultMembers());
    }

    public CouncilSettings WithPort(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1 to 65535.");
        }

        var builder = new UriBuilder(ListenPrefix)
        {
            Port = port
        };
        return new CouncilSettings(
            builder.Uri,
            MaxRounds,
            ConvergenceThreshold,
            MemberTimeout,
            AdvertisedModel,
            Members);
    }

    public override string ToString() => nameof(CouncilSettings);

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

    private static IReadOnlyList<CouncilMember> CreateDefaultMembers() =>
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
}
