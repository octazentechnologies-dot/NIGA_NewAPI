using Homeocentrum.Niga.NewAPI.Domain.Services.AudioCaseIntelligence.Embeddings;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.RubricIntelligence;

public class EmbeddingVectorMathTests
{
    [Fact]
    public void CosineSimilarity_ReturnsOne_ForIdenticalVectors()
    {
        var vector = new[] { 0.1f, 0.2f, 0.3f };
        var score = EmbeddingVectorMath.CosineSimilarity(vector, vector);
        Assert.InRange(score, 0.999f, 1.001f);
    }

    [Fact]
    public void ToScore_NormalizesCosineToZeroOneRange()
    {
        Assert.Equal(0.5m, EmbeddingVectorMath.ToScore(0f));
        Assert.Equal(1m, EmbeddingVectorMath.ToScore(1f));
    }
}
