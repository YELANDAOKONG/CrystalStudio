using CrystalStudio.Home;

using Xunit;

namespace CrystalStudio.Tests.Home;

public sealed class CouncilStoreTests
{
    [Fact]
    public void LoadOrCreate_WritesCodingAndWritingCouncils()
    {
        var root = UniqueRoot();
        try
        {
            var store = new CouncilStore(new StudioHome(root));
            var created = store.LoadOrCreate();

            Assert.True(
                File.Exists(Path.Combine(root, StudioHome.CouncilsDirectoryName, StudioHome.CodingCouncilFileName)));
            Assert.True(
                File.Exists(Path.Combine(root, StudioHome.CouncilsDirectoryName, StudioHome.WritingCouncilFileName)));
            Assert.Equal(2, created.Councils.Count);
            Assert.Equal("crystal-council", created.Default.AdvertisedModel);
            Assert.Contains(created.Councils, static council => council.AdvertisedModel == "crystal-writing");

            var loaded = store.Load();
            Assert.Equal(created.AdvertisedModels, loaded.AdvertisedModels);
            Assert.Equal(created.Default.Chair.Id, loaded.Default.Chair.Id);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void LoadOrCreate_DoesNotOverwriteAnExistingCouncilFile()
    {
        var root = UniqueRoot();
        try
        {
            var store = new CouncilStore(new StudioHome(root));
            store.LoadOrCreate();
            var codingPath = Path.Combine(
                root,
                StudioHome.CouncilsDirectoryName,
                StudioHome.CodingCouncilFileName);
            var original = File.ReadAllText(codingPath);
            File.WriteAllText(codingPath, original.Replace("careful analyst", "edited analyst", StringComparison.Ordinal));

            var reloaded = store.LoadOrCreate();
            Assert.Contains("edited analyst", reloaded.Default.Members[0].Persona, StringComparison.Ordinal);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public void Load_ReadsPerSeatThinking()
    {
        var root = UniqueRoot();
        try
        {
            var store = new CouncilStore(new StudioHome(root));
            store.LoadOrCreate();
            var codingPath = Path.Combine(
                root,
                StudioHome.CouncilsDirectoryName,
                StudioHome.CodingCouncilFileName);
            var json = File.ReadAllText(codingPath);
            json = json.Replace(
                "\"id\": \"analyst\"",
                "\"id\": \"analyst\",\n      \"thinking\": \"high\"",
                StringComparison.Ordinal);
            json = json.Replace(
                "\"id\": \"chair\"",
                "\"id\": \"chair\",\n      \"thinking\": \"off\"",
                StringComparison.Ordinal);
            File.WriteAllText(codingPath, json);

            var loaded = store.Load();
            Assert.Equal("high", loaded.Default.Members[0].Thinking);
            Assert.Equal("default", loaded.Default.Members[1].Thinking);
            Assert.Equal("off", loaded.Default.Chair.Thinking);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string UniqueRoot() =>
        Path.Combine(Path.GetTempPath(), "crystal-studio-tests", Guid.NewGuid().ToString("N"));

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
