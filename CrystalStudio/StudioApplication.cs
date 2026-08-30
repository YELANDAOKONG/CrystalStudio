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
            var council = new CouncilStore(studioHome).LoadOrCreate();
            if (options.Port is { } port)
            {
                council = council.WithPort(port);
            }

            using var factory = new MemberClientFactory(
                harnessSettings,
                new CredentialStore(harnessHome),
                PluginRegistry.CreateBuiltIn());
            WarmMembers(factory, council);

            using var listener = new CompatibleListener(council, factory);
            using var shutdown = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdown.Cancel();
            };

            WriteBanner(studioHome, harnessHome, council);
            await listener.RunAsync(shutdown.Token);
            return 0;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync(exception.Message);
            return 1;
        }
    }

    private static void WarmMembers(MemberClientFactory factory, CouncilSettings council)
    {
        foreach (var member in council.Members)
        {
            _ = factory.Create(member);
        }
    }

    private static void WriteBanner(
        StudioHome studioHome,
        CrystalHome harnessHome,
        CouncilSettings council)
    {
        Console.WriteLine($"Crystal Studio council listening on {council.ListenPrefix.AbsoluteUri}");
        Console.WriteLine($"Studio data: {studioHome.Root}");
        Console.WriteLine($"Harness data: {harnessHome.Root}");
        Console.WriteLine($"Advertised model: {council.AdvertisedModel}");
        foreach (var member in council.Members)
        {
            var chair = member.Id == council.Chair.Id ? " (chair)" : string.Empty;
            Console.WriteLine(
                $"  {member.Id}{chair}: {member.Provider}/{member.Model}");
        }
    }
}
