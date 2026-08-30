namespace CrystalStudio;

/// <summary>
/// Command-line overrides for the studio process.
/// </summary>
public sealed record StartupOptions
{
    public StartupOptions(string? studioHome, string? harnessHome, int? port, bool showHelp = false)
    {
        if (port is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(port), port, "Port must be 1 to 65535.");
        }

        StudioHome = studioHome;
        HarnessHome = harnessHome;
        Port = port;
        ShowHelp = showHelp;
    }

    public string? StudioHome { get; }

    public string? HarnessHome { get; }

    public int? Port { get; }

    public bool ShowHelp { get; }

    public static StartupOptions Parse(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        string? studioHome = null;
        string? harnessHome = null;
        int? port = null;

        for (var index = 0; index < args.Count; index++)
        {
            var argument = args[index];
            switch (argument)
            {
                case "--studio-home":
                    studioHome = ReadValue(args, ref index, argument);
                    break;
                case "--harness-home":
                case "--home":
                    harnessHome = ReadValue(args, ref index, argument);
                    break;
                case "--port":
                    var text = ReadValue(args, ref index, argument);
                    if (!int.TryParse(text, out var parsed))
                    {
                        throw new ArgumentException($"'{text}' is not a port number.", nameof(args));
                    }

                    port = parsed;
                    break;
                case "--help":
                case "-h":
                    return new StartupOptions(studioHome, harnessHome, port, showHelp: true);
                default:
                    throw new ArgumentException($"Unknown argument '{argument}'.", nameof(args));
            }
        }

        return new StartupOptions(studioHome, harnessHome, port);
    }

    public const string HelpText =
        "Crystal Studio model council. Options: --port, --studio-home, --harness-home.";

    public override string ToString() => nameof(StartupOptions);

    private static string ReadValue(IReadOnlyList<string> args, ref int index, string name)
    {
        index++;
        if (index >= args.Count || string.IsNullOrWhiteSpace(args[index]))
        {
            throw new ArgumentException($"Argument {name} needs a value.", nameof(args));
        }

        return args[index];
    }
}
