using Niga_Domain.DTOs;
using Niga_Domain.Services.AudioCaseIntelligence.Enterprise.Quality;
using Niga_Domain.Services.AudioCaseIntelligence.Merging;
using Xunit;

namespace Niga_Domain.Tests.RubricIntelligence;

public class LiveSubSectionMasterGateTests
{
    [Fact]
    public void Unresolved_positive_id_is_downgraded_not_shown_as_repertory()
    {
        var live = new Dictionary<int, string> { [10] = "MIND - ANXIETY" };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 10,
                SubSectionName = "MIND - ANXIETY",
                IsDbBacked = true,
                IsAiSuggested = false,
            },
            new()
            {
                SubSectionId = 999001,
                SubSectionName = "Stale cache name",
                IsDbBacked = true,
                IsAiSuggested = false,
                MatchSource = "Database",
            },
        };

        var result = LiveSubSectionMasterGate.Apply(rubrics, live, out var unresolved, out var renamed);

        Assert.Equal(1, unresolved);
        Assert.Equal(0, renamed);
        Assert.Equal(10, result[0].SubSectionId);
        Assert.True(result[0].IsDbBacked);

        Assert.Equal(0, result[1].SubSectionId);
        Assert.False(result[1].IsDbBacked);
        Assert.True(result[1].IsAiSuggested);
        Assert.Equal(LiveSubSectionMasterGate.UnresolvedMatchSource, result[1].MatchSource);
        Assert.Equal(EnterpriseRubricPresentationHelper.ResultKindAiConcept, result[1].ResultKind);
        Assert.Contains("999001", result[1].WhySuggested);
    }

    [Fact]
    public void Live_name_overwrites_stale_cached_name()
    {
        var live = new Dictionary<int, string> { [22] = "HEAD - PAIN - morning" };
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new()
            {
                SubSectionId = 22,
                SubSectionName = "HEAD - PAIN - mornings",
                IsDbBacked = true,
            },
        };

        var result = LiveSubSectionMasterGate.Apply(rubrics, live, out var unresolved, out var renamed);

        Assert.Equal(0, unresolved);
        Assert.Equal(1, renamed);
        Assert.Equal("HEAD - PAIN - morning", result[0].SubSectionName);
        Assert.True(result[0].IsDbBacked);
        Assert.Equal(22, result[0].SubSectionId);
    }

    [Fact]
    public void Zero_id_stays_non_db_backed()
    {
        var rubrics = new List<AudioCaseSuggestedRubricModel>
        {
            new() { SubSectionId = 0, SubSectionName = "AI - fear of heights", IsAiSuggested = true },
        };

        var result = LiveSubSectionMasterGate.Apply(rubrics, new Dictionary<int, string>(), out var unresolved, out _);

        Assert.Equal(0, unresolved);
        Assert.False(result[0].IsDbBacked);
        Assert.Equal(0, result[0].SubSectionId);
    }
}
