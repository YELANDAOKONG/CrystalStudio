using System.Text.Json.Nodes;

using CrystalStudio.Compatible;

using Xunit;

namespace CrystalStudio.Tests.Compatible;

public sealed class CompatibleWriterTests
{
    [Fact]
    public async Task WriteModelsAsync_ListsEveryAdvertisedId()
    {
        using var stream = new MemoryStream();
        var writer = new CompatibleWriter(stream, "models", "crystal-council", streamResponse: false);

        await writer.WriteModelsAsync(["crystal-council", "crystal-writing"], CancellationToken.None);

        stream.Position = 0;
        var document = JsonNode.Parse(stream);
        var ids = document!["data"]!.AsArray().Select(static node => node!["id"]!.GetValue<string>()).ToArray();
        Assert.Equal(["crystal-council", "crystal-writing"], ids);
    }
}
