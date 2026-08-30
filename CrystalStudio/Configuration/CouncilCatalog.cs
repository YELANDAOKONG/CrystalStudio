namespace CrystalStudio.Configuration;

/// <summary>
/// The process listen prefix plus every loaded council. Each council is one advertised model.
/// </summary>
public sealed record CouncilCatalog
{
    public const int DefaultPort = 18790;

    public CouncilCatalog(Uri listenPrefix, IReadOnlyList<CouncilSettings> councils)
    {
        ArgumentNullException.ThrowIfNull(listenPrefix);
        ArgumentNullException.ThrowIfNull(councils);

        if (!listenPrefix.IsAbsoluteUri)
        {
            throw new ArgumentException("Listen prefix must be absolute.", nameof(listenPrefix));
        }

        if (councils.Count < 1)
        {
            throw new ArgumentException("At least one council is required.", nameof(councils));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var council in councils)
        {
            ArgumentNullException.ThrowIfNull(council);
            if (!seen.Add(council.AdvertisedModel))
            {
                throw new ArgumentException(
                    $"Advertised model '{council.AdvertisedModel}' is listed more than once.",
                    nameof(councils));
            }
        }

        ListenPrefix = listenPrefix;
        Councils = councils;
        Default = ResolveDefault(councils);
    }

    public Uri ListenPrefix { get; }

    public IReadOnlyList<CouncilSettings> Councils { get; }

    public CouncilSettings Default { get; }

    public int Port => ListenPrefix.IsDefaultPort ? DefaultPort : ListenPrefix.Port;

    public IReadOnlyList<string> AdvertisedModels
    {
        get
        {
            var models = new string[Councils.Count];
            for (var index = 0; index < Councils.Count; index++)
            {
                models[index] = Councils[index].AdvertisedModel;
            }

            return models;
        }
    }

    public bool TryGet(string? model, out CouncilSettings settings)
    {
        if (string.IsNullOrWhiteSpace(model))
        {
            settings = Default;
            return true;
        }

        var requested = model.Trim();
        foreach (var council in Councils)
        {
            if (string.Equals(council.AdvertisedModel, requested, StringComparison.OrdinalIgnoreCase))
            {
                settings = council;
                return true;
            }
        }

        settings = Default;
        return false;
    }

    public CouncilCatalog WithPort(int port)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1 to 65535.");
        }

        var builder = new UriBuilder(ListenPrefix)
        {
            Port = port
        };
        return new CouncilCatalog(builder.Uri, Councils);
    }

    public static Uri CreateDefaultListenPrefix() =>
        new($"http://127.0.0.1:{DefaultPort}/");

    public override string ToString() => nameof(CouncilCatalog);

    private static CouncilSettings ResolveDefault(IReadOnlyList<CouncilSettings> councils)
    {
        foreach (var council in councils)
        {
            if (string.Equals(
                    council.AdvertisedModel,
                    CouncilSettings.DefaultAdvertisedModel,
                    StringComparison.OrdinalIgnoreCase))
            {
                return council;
            }
        }

        return councils[0];
    }
}
