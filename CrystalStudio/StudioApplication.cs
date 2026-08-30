using CrystalHarness.Home;
using CrystalHarness.Plugins;

using CrystalStudio.Adapters;
using CrystalStudio.Compatible;
using CrystalStudio.Configuration;
using CrystalStudio.Home;

namespace CrystalStudio;

/// <summary>
/// Loads Harness credentials, council seats, and the compatible listener.
/// </summary>
public static class StudioApplication
{
    public static async Task<int> RunAsync(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        try
        {
            var options = StartupOptions.Parse(args);
            if (options.ShowHelp)
            {
                Console.WriteLine(StartupOptions.HelpText);
                return 0;
            }

            var studioHome = StudioHome.Resolve(options.StudioHome);
            var harnessHome = CrystalHome.Resolve(options.HarnessHome);
            var harnessSettings = new SettingsStore(harnessHome).LoadOrCreate();
            var catalog = new CouncilStore(studioHome).LoadOrCreate();
            if (options.Port is { } port)
            {
                catalog = catalog.WithPort(port);
            }

            using var factory = new MemberClientFactory(
                harnessSettings,
                new CredentialStore(harnessHome),
                PluginRegistry.CreateBuiltIn());
            WarmMembers(factory, catalog);

            using var listener = new CompatibleListener(catalog, factory);
            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            WriteBanner(studioHome, harnessHome, catalog);
            await listener.RunAsync(shutdown.Token);
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 1;
        }
    }

    private static void WarmMembers(MemberClientFactory factory, CouncilCatalog catalog)
    {
        foreach (var council in catalog.Councils)
        {
            WarmCouncil(factory, council);
        }
    }

    private static void WarmCouncil(MemberClientFactory factory, CouncilSettings council)
    {
        foreach (var member in council.Members)
        {
            _ = factory.Create(member);
        }
    }

    private static void WriteBanner(
        StudioHome studioHome,
        CrystalHome harnessHome,
        CouncilCatalog catalog)
    {
        Console.WriteLine($"Crystal Studio council listening on {catalog.ListenPrefix.AbsoluteUri}");
        Console.WriteLine($"Studio data: {studioHome.Root}");
        Console.WriteLine($"Harness data: {harnessHome.Root}");
        Console.WriteLine($"Advertised models: {string.Join(", ", catalog.AdvertisedModels)}");
        foreach (var council in catalog.Councils)
        {
            WriteCouncil(council);
        }
    }

    private static void WriteCouncil(CouncilSettings council)
    {
        Console.WriteLine($"  {council.AdvertisedModel}:");
        foreach (var member in council.Members)
        {
            var chair = member.Id == council.Chair.Id ? " (chair)" : string.Empty;
            Console.WriteLine(
                $"    {member.Id}{chair}: {member.Provider}/{member.Model}");
        }
    }
}
