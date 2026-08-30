namespace CrystalStudio.Home;

/// <summary>
/// Resolves the <c>~/.Crystal/studio</c> data directory and its well-known files.
/// </summary>
public sealed class StudioHome
{
    public const string EnvironmentVariableName = "CRYSTAL_STUDIO_HOME";
    private const string ParentDirectoryName = ".Crystal";
    private const string DirectoryName = "studio";

    public StudioHome(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        Root = Path.GetFullPath(root);
    }

    public string Root { get; }

    public string CouncilPath => Path.Combine(Root, "council.json");

    public string LogsDirectory => Path.Combine(Root, "logs");

    public static StudioHome Resolve(string? root = null)
    {
        if (!string.IsNullOrWhiteSpace(root))
        {
            return new StudioHome(root);
        }

        var fromEnvironment = Environment.GetEnvironmentVariable(EnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return new StudioHome(fromEnvironment);
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(userProfile))
        {
            throw new InvalidOperationException("The user profile directory is not available.");
        }

        return new StudioHome(Path.Combine(userProfile, ParentDirectoryName, DirectoryName));
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(LogsDirectory);
    }
}
