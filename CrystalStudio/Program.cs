namespace CrystalStudio;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await StudioApplication.RunAsync(args);
    }
}
