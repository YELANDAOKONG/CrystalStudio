using CrystalStudio.Council;

using Xunit;

namespace CrystalStudio.Tests.Council;

public sealed class LexicalSimilarityTests
{
    [Fact]
    public void Cosine_IsOneForIdenticalText()
    {
        Assert.Equal(1, LexicalSimilarity.Cosine("hello world", "hello world"), 5);
    }

    [Fact]
    public void Cosine_IsZeroForDisjointTokens()
    {
        Assert.Equal(0, LexicalSimilarity.Cosine("alpha beta", "gamma delta"), 5);
    }

    [Fact]
    public void AveragePairwise_IsOneForASingleText()
    {
        Assert.Equal(1, LexicalSimilarity.AveragePairwise(["only"]));
    }
}
