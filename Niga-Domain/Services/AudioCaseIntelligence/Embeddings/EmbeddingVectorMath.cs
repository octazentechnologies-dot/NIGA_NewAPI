namespace Niga_Domain.Services.AudioCaseIntelligence.Embeddings;

public static class EmbeddingVectorMath
{
    public static float CosineSimilarity(IReadOnlyList<float> left, IReadOnlyList<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
        {
            return 0f;
        }

        double dot = 0;
        double magLeft = 0;
        double magRight = 0;

        for (var i = 0; i < left.Count; i++)
        {
            dot += left[i] * right[i];
            magLeft += left[i] * left[i];
            magRight += right[i] * right[i];
        }

        if (magLeft == 0 || magRight == 0)
        {
            return 0f;
        }

        return (float)(dot / (Math.Sqrt(magLeft) * Math.Sqrt(magRight)));
    }

    public static decimal ToScore(float cosine)
    {
        var normalized = (cosine + 1f) / 2f;
        var value = (decimal)normalized;
        if (value < 0m) return 0m;
        if (value > 1m) return 1m;
        return Math.Round(value, 4);
    }
}
