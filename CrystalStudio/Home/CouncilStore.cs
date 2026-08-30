using System.Text.Json;

using CrystalStudio.Configuration;

namespace CrystalStudio.Home;

/// <summary>
/// Reads and writes <c>council.json</c> under the Studio home directory.
/// </summary>
public sealed class CouncilStore
{
    private readonly StudioHome _home;

    public CouncilStore(StudioHome home)
    {
        ArgumentNullException.ThrowIfNull(home);
        _home = home;
    }

    public CouncilSettings LoadOrCreate()
    {
        _home.EnsureCreated();
        if (!File.Exists(_home.CouncilPath))
        {
            var created = CouncilSettings.CreateDefault();
            Save(created);
            return created;
        }

        return Load();
    }

    public CouncilSettings Load()
    {
        var json = File.ReadAllText(_home.CouncilPath);
        var document = JsonSerializer.Deserialize<CouncilDocument>(json, StudioJson.Options)
            ?? new CouncilDocument();
        var defaults = CouncilSettings.CreateDefault();
        var listen = string.IsNullOrWhiteSpace(document.Listen)
            ? defaults.ListenPrefix
            : new Uri(document.Listen, UriKind.Absolute);
        var members = ReadMembers(document.Members, defaults);

        return new CouncilSettings(
            listen,
            document.MaxRounds ?? defaults.MaxRounds,
            document.ConvergenceThreshold ?? defaults.ConvergenceThreshold,
            TimeSpan.FromSeconds(
                document.MemberTimeoutSeconds ?? (int)defaults.MemberTimeout.TotalSeconds),
            string.IsNullOrWhiteSpace(document.Model)
                ? defaults.AdvertisedModel
                : document.Model.Trim(),
            members);
    }

    public void Save(CouncilSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _home.EnsureCreated();

        var document = new CouncilDocument
        {
            Listen = settings.ListenPrefix.AbsoluteUri,
            MaxRounds = settings.MaxRounds,
            ConvergenceThreshold = settings.ConvergenceThreshold,
            MemberTimeoutSeconds = (int)settings.MemberTimeout.TotalSeconds,
            Model = settings.AdvertisedModel,
            Members = WriteMembers(settings.Members)
        };
        var json = JsonSerializer.Serialize(document, StudioJson.Options);
        File.WriteAllText(_home.CouncilPath, json);
    }

    private static IReadOnlyList<CouncilMember> ReadMembers(
        List<MemberDocument>? document,
        CouncilSettings defaults)
    {
        if (document is null || document.Count == 0)
        {
            return defaults.Members;
        }

        var members = new List<CouncilMember>(document.Count);
        foreach (var entry in document)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (string.IsNullOrWhiteSpace(entry.Id)
                || string.IsNullOrWhiteSpace(entry.Persona)
                || string.IsNullOrWhiteSpace(entry.Provider)
                || string.IsNullOrWhiteSpace(entry.Model))
            {
                throw new InvalidOperationException(
                    "Each council member needs id, persona, provider, and model.");
            }

            members.Add(
                new CouncilMember(
                    entry.Id,
                    entry.Persona,
                    entry.Provider,
                    entry.Model,
                    entry.Chair ?? false));
        }

        return members;
    }

    private static List<MemberDocument> WriteMembers(IReadOnlyList<CouncilMember> members)
    {
        var document = new List<MemberDocument>(members.Count);
        foreach (var member in members)
        {
            document.Add(
                new MemberDocument
                {
                    Id = member.Id,
                    Persona = member.Persona,
                    Provider = member.Provider,
                    Model = member.Model,
                    Chair = member.Chair ? true : null
                });
        }

        return document;
    }
}
