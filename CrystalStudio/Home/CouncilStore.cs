using System.Text.Json;

using CrystalStudio.Configuration;

namespace CrystalStudio.Home;

/// <summary>
/// Reads and writes council files under <c>councils/</c> in the Studio home directory.
/// </summary>
public sealed class CouncilStore
{
    private readonly StudioHome _home;

    public CouncilStore(StudioHome home)
    {
        ArgumentNullException.ThrowIfNull(home);
        _home = home;
    }

    public CouncilCatalog LoadOrCreate()
    {
        _home.EnsureCreated();
        EnsureDefault(
            StudioHome.CodingCouncilFileName,
            CouncilSettings.CreateDefault());
        EnsureDefault(
            StudioHome.WritingCouncilFileName,
            CouncilSettings.CreateWritingDefault());
        return Load();
    }

    public CouncilCatalog Load()
    {
        var files = Directory.GetFiles(_home.CouncilsDirectory, "*.json");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        if (files.Length == 0)
        {
            throw new InvalidOperationException(
                $"No council files were found in '{_home.CouncilsDirectory}'.");
        }

        Uri? listen = null;
        var councils = new List<CouncilSettings>(files.Length);
        foreach (var path in files)
        {
            var document = ReadDocument(path);
            listen = ReadListen(document.Listen, listen, path);
            councils.Add(ReadCouncil(document, path));
        }

        return new CouncilCatalog(listen ?? CouncilCatalog.CreateDefaultListenPrefix(), councils);
    }

    private void EnsureDefault(string fileName, CouncilSettings settings)
    {
        var path = Path.Combine(_home.CouncilsDirectory, fileName);
        if (File.Exists(path) || Advertises(settings.AdvertisedModel))
        {
            return;
        }

        WriteFile(path, settings);
    }

    private bool Advertises(string advertisedModel)
    {
        if (!Directory.Exists(_home.CouncilsDirectory))
        {
            return false;
        }

        foreach (var path in Directory.GetFiles(_home.CouncilsDirectory, "*.json"))
        {
            var document = ReadDocument(path);
            var model = ResolveModel(document.Model, path);
            if (string.Equals(model, advertisedModel, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void WriteFile(string path, CouncilSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);

        var document = new CouncilDocument
        {
            MaxRounds = settings.MaxRounds,
            ConvergenceThreshold = settings.ConvergenceThreshold,
            MemberTimeoutSeconds = (int)settings.MemberTimeout.TotalSeconds,
            Model = settings.AdvertisedModel,
            Members = WriteMembers(settings.Members)
        };
        var json = JsonSerializer.Serialize(document, StudioJson.Options);
        File.WriteAllText(path, json);
    }

    private static CouncilDocument ReadDocument(string path)
    {
        var json = File.ReadAllText(path);
        try
        {
            return JsonSerializer.Deserialize<CouncilDocument>(json, StudioJson.Options)
                ?? new CouncilDocument();
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Council file '{path}' is not valid JSON: {exception.Message}",
                exception);
        }
    }

    private static Uri? ReadListen(string? listen, Uri? current, string path)
    {
        if (string.IsNullOrWhiteSpace(listen))
        {
            return current;
        }

        var parsed = new Uri(listen, UriKind.Absolute);
        if (current is not null
            && !string.Equals(current.AbsoluteUri, parsed.AbsoluteUri, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Council file '{path}' has listen '{parsed.AbsoluteUri}', "
                + $"which does not match '{current.AbsoluteUri}'.");
        }

        return parsed;
    }

    private static CouncilSettings ReadCouncil(CouncilDocument document, string path)
    {
        var defaults = CouncilSettings.CreateDefault();
        var members = ReadMembers(document.Members, defaults);
        return new CouncilSettings(
            document.MaxRounds ?? defaults.MaxRounds,
            document.ConvergenceThreshold ?? defaults.ConvergenceThreshold,
            TimeSpan.FromSeconds(
                document.MemberTimeoutSeconds ?? (int)defaults.MemberTimeout.TotalSeconds),
            ResolveModel(document.Model, path),
            members);
    }

    private static string ResolveModel(string? model, string path)
    {
        if (!string.IsNullOrWhiteSpace(model))
        {
            return model.Trim();
        }

        var fromFile = Path.GetFileNameWithoutExtension(path);
        if (string.IsNullOrWhiteSpace(fromFile))
        {
            throw new InvalidOperationException(
                $"Council file '{path}' needs a model id.");
        }

        return fromFile;
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
