using Homeocentrum.Niga.NewAPI.Domain.DTOs;
using Homeocentrum.Niga.NewAPI.Domain.Repositories.AiEmbeddingInfrastructure;
using Xunit;

namespace Homeocentrum.Niga.NewAPI.Domain.Tests.AiEmbeddingInfrastructure;

public class RepertoryEmbeddingChangeDetectorTests
{
    [Fact]
    public void MergeCandidate_deleted_change_takes_priority()
    {
        var changes = new Dictionary<int, RubricEmbeddingChangeCandidate>();
        RepertoryEmbeddingChangeDetector.AddChange(
            changes,
            10,
            AiEmbeddingQueueChangeTypes.Updated,
            DateTime.UtcNow,
            c => c.Reasons.Add(AiEmbeddingChangeReasons.UpdatedDate));

        RepertoryEmbeddingChangeDetector.AddChange(
            changes,
            10,
            AiEmbeddingQueueChangeTypes.Deleted,
            DateTime.UtcNow,
            c => c.Reasons.Add(AiEmbeddingChangeReasons.DeletedDate));

        var candidate = changes[10];
        Assert.Equal(AiEmbeddingQueueChangeTypes.Deleted, candidate.ChangeType);
        Assert.True(candidate.RequiresArchive);
        Assert.False(candidate.RequiresEmbedding);
        Assert.Contains(AiEmbeddingChangeReasons.UpdatedDate, candidate.Reasons);
        Assert.Contains(AiEmbeddingChangeReasons.DeletedDate, candidate.Reasons);
    }

    [Fact]
    public void MergeCandidate_deduplicates_reasons()
    {
        var existing = new RubricEmbeddingChangeCandidate
        {
            RubricId = 20,
            ChangeType = AiEmbeddingQueueChangeTypes.EnrichmentChanged,
            Reasons = { AiEmbeddingChangeReasons.NewSynonym },
        };

        RepertoryEmbeddingChangeDetector.MergeCandidate(
            existing,
            AiEmbeddingQueueChangeTypes.EnrichmentChanged,
            c =>
            {
                c.Reasons.Add(AiEmbeddingChangeReasons.NewSynonym);
                c.Reasons.Add(AiEmbeddingChangeReasons.HashComparison);
            });

        Assert.Equal(2, existing.Reasons.Count);
        Assert.Contains(AiEmbeddingChangeReasons.HashComparison, existing.Reasons);
    }

    [Fact]
    public void NormalizeDate_treats_null_as_min()
    {
        var value = RepertoryEmbeddingChangeDetector.NormalizeDate(null);
        Assert.Equal(DateTime.MinValue, value);
    }
}
